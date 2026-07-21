# 作品史カタログ — 書籍・絵画・彫刻・建築・音楽・演劇・映画

## この台帳でいう「すべて」

歴史上の作品の総数は確定できません。失われた作品、無名・共同制作、未刊行資料、
口承や即興、同じ伝承の異本、現在も変化する実践があるためです。本作では網羅を
一度で断言せず、出典を確認でき、ゲーム内で識別可能な作品を均衡の取れた単位で
順次追加します。

第1弾150件に演劇・映画各30件を加えた210件を固定し、さらに7分野へ
6地域から各1件ずつ42件を後方追加しました。現在は **252件** です。

| 分野 | 件数 | 収録範囲 |
|---|---:|---|
| 書籍 | 36 | 粘土板、写本、巻物、口承叙事詩、歴史書、戯曲、小説を含む |
| 絵画 | 36 | 壁画、絵巻、屏風、木版画、樹皮画を含む |
| 彫刻 | 36 | 丸彫、浮彫、記念碑、儀礼造形、共同体の彫刻伝統を含む |
| 建築 | 36 | 単体建築、都市・祭祀複合体、景観と一体の建築群を含む |
| 音楽 | 36 | 記譜作品、組曲、歌唱・器楽・舞踊と結び付く継承伝統を含む |
| 演劇 | 36 | 戯曲、仮面劇、人形劇、影絵、舞踊劇、即興演劇を含む |
| 映画 | 36 | 無声映画、劇映画、実験映画、記録映画、共同製作を含む |

地域は既存の世界史台帳と同じ6区分を使い、各地域42件（各分野6件）です。

- アフリカ
- 西・南アジア
- 東・東南アジア
- ヨーロッパ・地中海
- アメリカ大陸
- オセアニア

全252件の名称・時期・作者／担い手・要約・関連文明／偉人は
`Assets/Scripts/Core/MasterpieceCatalog.cs` にあり、ゲーム内の
「世界史図鑑」→「作品史」でも地域別に閲覧できます。

## 選定原則

1. 「最高傑作」の順位付けではなく、時代・媒体・制作主体の幅を優先する。
2. 作者不詳・共同体制作・生きた伝統も、個人名のある作品と同じ台帳で扱う。
3. 後世の国家・文明を便宜的に遡及させない。関係が直接でない場合、関連文明IDは空にする。
4. 題名、年代、帰属に複数説がある場合は、断定を避ける表現を使う。
5. 固定IDはセーブ互換のため変更しない。修正は表示名・説明・関連付けで行う。

## ゲームへの接続

- 都市・文化政策・登用済み偉人から毎ターン作品ポイントを得る。
- 作品は世界全体で一度だけ収蔵でき、AI文明も候補から自動収蔵する。
- 関連文明は費用20%、同地域は10%、関連偉人登用済みは追加15%軽減（合計上限30%）。
- 関連偉人を登用すると、その人物に直接結び付く未収蔵作品を1件無償で収蔵する。
- 書籍は科学、絵画・音楽は文化的影響力、彫刻・建築は都市生産を中心に強化する。
- 演劇は文化・都市生産・影響力、映画は文化・科学・影響力を複合的に強化する。
- 作品ポイント、累計、収蔵済みIDはセーブversion 9で保存する。version 8以前も読める。

## 調査の入口

カタログ化では、単一機関の価値判断を世界史そのものとはみなしません。次の公的・
学術的な登録・コレクションを、名称・年代・地域・媒体を照合する入口として使います。

- UNESCO Memory of the World（文書遺産）: https://www.unesco.org/en/memory-world/grid
- The Metropolitan Museum of Art, Heilbrunn Timeline of Art History: https://www.metmuseum.org/essays/timeline-of-art-history
- The Met Collection / Open Access: https://www.metmuseum.org/art/collection
- Library of Congress Catalog: https://catalog.loc.gov/
- UNESCO World Heritage List（建築・都市・文化的景観）: https://whc.unesco.org/en/list/
- UNESCO Intangible Cultural Heritage Lists（音楽を含む実践）: https://ich.unesco.org/en/lists
- UNESCO Intangible Cultural Heritage（演劇・人形劇・舞踊劇等）: https://ich.unesco.org/en/lists
- Library of Congress National Film Registry: https://www.loc.gov/programs/national-film-preservation-board/film-registry/complete-national-film-registry-listing/
- BFI, Sight and Sound film polls: https://www.bfi.org.uk/sight-and-sound/greatest-films-all-time
- National Film and Sound Archive of Australia: https://www.nfsa.gov.au/collection

今回の42件では、たとえば次の所蔵機関・公的記録で名称、年代、制作主体、説明を
個別に確認しました。

- UNESCO「ファシル・ゲビ」: https://whc.unesco.org/en/list/19
- UNESCO「琉球王国のグスク及び関連遺産群」: https://whc.unesco.org/en/list/972/
- UNESCO「トンガのラカラカ」: https://ich.unesco.org/en/RL/lakalaka-dances-and-sung-speeches-of-tonga-00072
- UNESCO「組踊」: https://ich.unesco.org/en/RL/kumiodori-traditional-okinawan-musical-theatre-00405
- The Met「イドゥア王母のペンダント仮面」: https://www.metmuseum.org/art/collection/search/318622
- National Gallery「アルノルフィーニ夫妻像」: https://www.nationalgallery.org.uk/paintings/jan-van-eyck-the-arnolfini-portrait
- Library of Congress「ジェイコブ・ローレンス」: https://guides.loc.gov/jacob-lawrence
- National Gallery of Australia「アボリジナル・メモリアル」: https://nga.gov.au/first-nations/the-aboriginal-memorial/
- Festival de Cannes「Moolaadé」: https://www.festival-cannes.com/f/moolaade/
- New Zealand Film Commission「Whale Rider」: https://www.nzfilm.co.nz/films/whale-rider
- Danish Film Institute「1920–1929」: https://www.dfi.dk/en/node/30877

## 次回以降の追加手順

次のバッチも、7分野・6地域の偏りを点検して追加します。既存252件のIDと順序は
固定し、候補ごとに重複、異名、
年代、制作者表記、現存／継承状況、既存文明・偉人との直接関係を確認し、専用テストで
ID一意性と参照整合性を検証します。これにより「全部」を虚偽なく、一件ずつ拡張します。
