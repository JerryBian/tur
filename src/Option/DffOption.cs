namespace Tur.Option;

public class DffOption : OptionBase
{
    public DffOption(string[] args) : base(args, "dff")
    {
    }

    public string Dir { get; set; }
}