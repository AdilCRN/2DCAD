namespace MRecipeStructure.Classes.MRecipeStructure
{
    public struct MArrayPositionInfo
    {
		public int Index { get; set; }
		public double Offset { get; set; }

        public MArrayPositionInfo(int index, double offset)
        {
            Index = index;
            Offset = offset;
        }
    }
}
