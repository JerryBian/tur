namespace Tur.Option;

public class RmOption : OptionBase
{
    public RmOption(string[] args) : base(args, "rm")
    {
    }

    public bool File { get; set; }

    public bool Dir { get; set; }

    public bool EmptyDir { get; set; }

    public string FromFile { get; set; }

    public string Destination { get; set; }
}