using System;

namespace Deucarian.Theming
{
    /// <summary>Marks an authoritative set of named AudioRoleKey fields or properties for the Inspector.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AudioRoleKeySetAttribute : Attribute { }
}
