using System.Linq;

namespace HotspotMaker.Hotspot
{
    public class TextureGroupVM
    {
        public string Name { get; }
        public object[] Children { get; }
        public int TextureCount { get; }


        public TextureGroupVM(string name, object[] children)
        {
            Name = name;
            Children = children;
            TextureCount = children.Select(GetTextureCount).Sum();
        }


        private static int GetTextureCount(object child)
        {
            if (child is TextureGroupVM groupVM)
                return groupVM.TextureCount;
            else
                return 1;
        }
    }
}
