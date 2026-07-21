using System;

namespace HexCiv.Core
{
    [Serializable]
    public struct Yields
    {
        public int Food;
        public int Production;
        public int Science;

        public Yields(int food, int production, int science = 0)
        {
            Food = food;
            Production = production;
            Science = science;
        }

        public static Yields operator +(Yields a, Yields b)
            => new Yields(a.Food + b.Food, a.Production + b.Production, a.Science + b.Science);

        public override string ToString() => $"食料{Food} 生産{Production} 科学{Science}";
    }
}
