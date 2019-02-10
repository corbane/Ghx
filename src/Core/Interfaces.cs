using System;

namespace Ghx.RoslynScript
{
    public interface IOAttribute
    {
        string NickName { get; }
        string Description { get; }
        object DefaultValue { get; }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class Input : Attribute, IOAttribute
    {
        public string NickName { get; set; }

        public string Description { get; set; }

        public object DefaultValue { get; set; }
        
        public Input(string nickname, string description = "", object defaultValue = null)
        {
            NickName = nickname;
            Description = description;
            DefaultValue = defaultValue;
        }
    }
    
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class Output : Attribute, IOAttribute
    {
        public string NickName { get; set; }

        public string Description { get; set; }

        public object DefaultValue { get; set; }

        public Output(string nickname, string description = "", object defaultValue = null)
        {
            NickName = nickname;
            Description = description;
            DefaultValue = defaultValue;
        }
    }
}
