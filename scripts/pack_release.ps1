<#
.SYNOPSIS
    HexCiv の Windows ビルドを配布用 zip に固め、検証用の manifest を出す。

.DESCRIPTION
    2026-07-28 Claude Code 追加。有償配布の前提となる「売り物のパッケージ」を作る。
    配信経路(Supabase / BOOTH / GitHub Releases)には依存しない。

    やること:
      1. Build/ の中身を検証する(実行ファイル・データフォルダの実在)
      2. 配布に不要なファイルを除いて zip に固める
      3. SHA256・バイト数・生成時刻を manifest(JSON)に書く

    manifest の SHA256 は、購入者が落としたファイルの同一性を確認するためと、
    こちらが「配信中のファイルが意図した版か」を後から突き合わせるために出す。
    容量とハッシュを控えずに配信すると、差し替え事故に後から気づけない。

.PARAMETER Version
    パッケージの版番号。省略時は ProjectSettings の bundleVersion を読む。

.PARAMETER BuildDir
    Unity の出力先。既定はリポジトリ直下の Build。

.PARAMETER OutDir
    zip と manifest の出力先。既定は dist(.gitignore 済み)。

.EXAMPLE
    pwsh -File scripts/pack_release.ps1
    pwsh -File scripts/pack_release.ps1 -Version 1.1
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$BuildDir,
    [string]$OutDir
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $BuildDir) { $BuildDir = Join-Path $repoRoot 'Build' }
if (-not $OutDir)   { $OutDir   = Join-Path $repoRoot 'dist' }

# ---- 版番号 ----
if (-not $Version) {
    $settings = Join-Path $repoRoot 'ProjectSettings\ProjectSettings.asset'
    if (-not (Test-Path $settings)) { throw "ProjectSettings が見つかりません: $settings" }
    $line = Select-String -Path $settings -Pattern '^\s*bundleVersion:\s*(.+)$' | Select-Object -First 1
    if (-not $line) { throw 'bundleVersion を ProjectSettings から読めませんでした' }
    $Version = $line.Matches[0].Groups[1].Value.Trim()
}
if ($Version -notmatch '^[0-9A-Za-z.\-_]+$') {
    throw "版番号にファイル名として使えない文字が含まれています: $Version"
}

# ---- ビルドの検証(空の zip を配ってしまう事故を防ぐ) ----
if (-not (Test-Path $BuildDir)) { throw "ビルドがありません: $BuildDir" }

$exe = Join-Path $BuildDir 'HexCiv.exe'
$dataDir = Join-Path $BuildDir 'HexCiv_Data'
if (-not (Test-Path $exe))     { throw "実行ファイルがありません: $exe" }
if (-not (Test-Path $dataDir)) { throw "データフォルダがありません: $dataDir" }

# 起動に必須の管理アセンブリ。ここが欠けた zip は起動できないので事前に落とす。
$managed = Join-Path $dataDir 'Managed\Assembly-CSharp.dll'
if (-not (Test-Path $managed)) { throw "管理アセンブリがありません: $managed" }

# ---- 配布に不要なもの ----
# クラッシュレポータは配布しても害はないが、ユーザー体験上は落ちたときに
# 見知らぬダイアログが出るだけなので同梱しない。
$excludeNames = @('UnityCrashHandler64.exe')
# デバッグシンボル(.pdb)は容量だけ食って購入者には無価値。
$excludeExtensions = @('.pdb')

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$stageRoot = Join-Path $OutDir ".stage_$Version"
if (Test-Path $stageRoot) { Remove-Item -Recurse -Force $stageRoot }
$stageDir = Join-Path $stageRoot "HexCiv-v$Version-win64"
New-Item -ItemType Directory -Force -Path $stageDir | Out-Null

try {
    # ---- ステージングへコピー(除外しつつ) ----
    $copied = 0
    $skipped = 0
    Get-ChildItem -Path $BuildDir -Recurse -File | ForEach-Object {
        $name = $_.Name
        $ext = $_.Extension.ToLowerInvariant()
        if ($excludeNames -contains $name -or $excludeExtensions -contains $ext) {
            $skipped++
            return
        }
        $relative = $_.FullName.Substring($BuildDir.Length).TrimStart('\', '/')
        $target = Join-Path $stageDir $relative
        $targetDir = Split-Path -Parent $target
        if (-not (Test-Path $targetDir)) { New-Item -ItemType Directory -Force -Path $targetDir | Out-Null }
        Copy-Item -LiteralPath $_.FullName -Destination $target -Force
        $copied++
    }

    # 同梱物: ライセンスと最低限の説明。何を買ったのかが zip 内で完結するようにする。
    foreach ($doc in @('LICENSE.md', 'README.md')) {
        $src = Join-Path $repoRoot $doc
        if (Test-Path $src) { Copy-Item -LiteralPath $src -Destination (Join-Path $stageDir $doc) -Force }
    }

    # ---- zip ----
    $zipPath = Join-Path $OutDir "HexCiv-v$Version-win64.zip"
    if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
    Compress-Archive -Path $stageDir -DestinationPath $zipPath -CompressionLevel Optimal

    # ---- manifest ----
    $zipItem = Get-Item $zipPath
    $hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $manifest = [ordered]@{
        product     = 'hexciv'
        version     = $Version
        platform    = 'win64'
        fileName    = $zipItem.Name
        sizeBytes   = $zipItem.Length
        sha256      = $hash
        fileCount   = $copied
        excluded    = $skipped
        # 生成時刻は UTC の ISO8601。配信中のファイルと突き合わせるときの手掛かり。
        builtAtUtc  = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    }
    $manifestPath = Join-Path $OutDir "HexCiv-v$Version-win64.manifest.json"
    $manifest | ConvertTo-Json | Out-File -FilePath $manifestPath -Encoding utf8

    Write-Output "PACK OK"
    Write-Output ("  zip      : {0}" -f $zipPath)
    Write-Output ("  size     : {0:N0} bytes ({1:N1} MB)" -f $zipItem.Length, ($zipItem.Length / 1MB))
    Write-Output ("  sha256   : {0}" -f $hash)
    Write-Output ("  files    : {0} (除外 {1})" -f $copied, $skipped)
    Write-Output ("  manifest : {0}" -f $manifestPath)
}
finally {
    if (Test-Path $stageRoot) { Remove-Item -Recurse -Force $stageRoot }
}
