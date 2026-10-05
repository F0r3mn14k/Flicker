using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace Flicker.Mac;

public sealed class Rule
{
    public bool IsPause { get; set; }
    public bool Enabled { get; set; } = true;
    public bool AllowDuringPause { get; set; }
    public string Key { get; set; } = "Space";
    public string Mode { get; set; } = "Co ile sekund";
    public double Seconds { get; set; } = 1;
    public string Time { get; set; } = "12:00:00";
    public double PauseMinutes { get; set; } = 10;
    public int HoldMs { get; set; } = 80;
    public int MouseX { get; set; }
    public int MouseY { get; set; }
    [XmlIgnore] public double Next { get; set; }
    [XmlIgnore] public int Count { get; set; }
    [XmlIgnore] public DateOnly LastDay { get; set; }

    public override string ToString() => IsPause ? $"Wstrzymaj · {Time}" : $"{Key}{(AllowDuringPause ? " ★ Master" : "")}";
}

public sealed class SequenceStep
{
    public bool Wait { get; set; }
    public string Key { get; set; } = "Space";
    public double Seconds { get; set; } = 1;
    public int HoldMs { get; set; } = 80;
    public int MouseX { get; set; }
    public int MouseY { get; set; }
}

public sealed class SequenceDocument
{
    public int Repeats { get; set; } = 1;
    public List<SequenceStep> Steps { get; set; } = [];
}

public sealed class FlickerProfile
{
    public int FormatVersion { get; set; } = 1;
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Domyślny";
    public bool Pinned { get; set; }
    public List<Rule> Rules { get; set; } = [];
    public SequenceDocument Sequence { get; set; } = new();
    public override string ToString() => Pinned ? $"★ {Name}" : Name;
}

public sealed class ProfileLibrary
{
    public int FormatVersion { get; set; } = 1;
    public string ActiveId { get; set; } = "";
    public List<FlickerProfile> Profiles { get; set; } = [];
    [XmlIgnore] public FlickerProfile? Active => Profiles.FirstOrDefault(p => p.Id == ActiveId);

    public IEnumerable<FlickerProfile> Ordered() => Profiles.OrderByDescending(p => p.Pinned).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase);
}

public static class ProfileStorage
{
    public static ProfileLibrary CreateDefault()
    {
        var profile = new FlickerProfile();
        return new ProfileLibrary { ActiveId = profile.Id, Profiles = [profile] };
    }

    public static ProfileLibrary ReadLibrary(string path)
    {
        using var stream = File.OpenRead(path);
        return (ProfileLibrary)new XmlSerializer(typeof(ProfileLibrary)).Deserialize(stream)!;
    }

    public static void WriteLibrary(string path, ProfileLibrary library)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = File.Create(path);
        new XmlSerializer(typeof(ProfileLibrary)).Serialize(stream, library);
    }

    public static FlickerProfile ReadProfile(string path)
    {
        using var stream = File.OpenRead(path);
        return (FlickerProfile)new XmlSerializer(typeof(FlickerProfile)).Deserialize(stream)!;
    }

    public static void WriteProfile(string path, FlickerProfile profile)
    {
        using var stream = File.Create(path);
        new XmlSerializer(typeof(FlickerProfile)).Serialize(stream, profile);
    }
}
