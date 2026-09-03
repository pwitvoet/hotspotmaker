namespace HotspotMaker.Hotspot
{
    public class TextureGroupVM
    {
        public string Name { get; }
        public object[] Children { get; }


        public TextureGroupVM(string name, object[] children)
        {
            Name = name;
            Children = children;
        }
    }
}
