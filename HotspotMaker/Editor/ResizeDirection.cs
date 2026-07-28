using System;

namespace HotspotMaker.Editor
{
    [Flags]
    public enum ResizeDirection
    {
        None =          0,

        Top =           0x01,
        Right =         0x02,
        Bottom =        0x04,
        Left =          0x08,

        TopLeft =       Top | Left,
        TopRight =      Top | Right,
        BottomRight =   Bottom | Right,
        BottomLeft =    Bottom | Left,

        Move =          0x10,
    }
}
