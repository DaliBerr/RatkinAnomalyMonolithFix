using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using System.Collections.Immutable;

string root = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
string game = args.Length > 1 ? args[1] : @"C:\Program Files (x86)\Steam\steamapps\common\RimWorld";
string dependency = args.Length > 2 ? args[2] : Path.GetFullPath(Path.Combine(root, "../Rimworld_Assmb/RatkinAnomaly/3293914637"));
int count = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    count++;
}
XElement metadata = XDocument.Load(Path.Combine(root, "About/About.xml")).Root;
XElement load = XDocument.Load(Path.Combine(root, "LoadFolders.xml")).Root;
foreach (string version in new[] { "1.5", "1.6", "1.7" })
{
    string managed = version == "1.7" ? game : Path.Combine(root, version, "ExternalMods");
    using AssemblyData core = new(Path.Combine(managed, "Assembly-CSharp.dll"));
    using AssemblyData ratkin = new(Path.Combine(dependency, version, "Assemblies/RatkinAnomaly.dll"));
    using AssemblyData mod = new(Path.Combine(root, version, "Assemblies/RatkinAnomalyMonolithFix.dll"));
    Check(core.Version.Minor == int.Parse(version.Split('.')[1]), "Wrong original game version");
    Check(ratkin.Name == "RatkinAnomaly", "Wrong Ratkin dependency");
    Check(ratkin.Method("RatkinAnomaly.RAComponent", "EndPlay", Array.Empty<string>(), "System.Void", false), "Ratkin ending target changed");
    Check(ratkin.Field("RatkinAnomaly.RAComponent", "actor", "Verse.Pawn"), "Ratkin actor reference changed");
    Check(core.Method("RimWorld.Utility.VoidAwakeningUtility", "DisruptTheLink", new[] { "pawn" }, "System.Void", true), "Original ending target changed");
    Check(core.Method("RimWorld.Utility.VoidAwakeningUtility", "CloseMetalHell", new[] { "pawn" }, "System.Void", true), "Private ending delegate target changed");
    Check(core.Method("RimWorld.Building_VoidMonolith", "Collapse", Array.Empty<string>(), "System.Void", false), "Monolith collapse API changed");
    Check(mod.HasType("RatkinAnomalyMonolithFix.Core.ComponentData"), "Legacy save component identity changed");
    Check(mod.Field("RatkinAnomalyMonolithFix.Core.Data", "HeroPawn", "Verse.Pawn"), "Saved hero type changed");
    var references = mod.References();
    Check(references["0Harmony"].Major == (version == "1.7" ? 3 : 2), "Wrong Harmony major version");
    Check(!references.ContainsKey("RatkinAnomaly"), "Patch is hard-bound to a different Ratkin binary's game references");
    Check(metadata.Element("supportedVersions").Elements("li").Any(element => element.Value == version), "Supported version missing");
    string[] folders = load.Element("v" + version).Elements("li").Select(element => element.Value).ToArray();
    Check(folders.Contains("Common") && folders.Contains(version), "Version or shared translations are not loaded");
    string[] dlls = folders.SelectMany(folder => {
        string path = Path.Combine(root, folder == "/" ? "" : folder, "Assemblies");
        return Directory.Exists(path) ? Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories) : Array.Empty<string>();
    }).ToArray();
    Check(dlls.Length == 1 && Path.GetFileName(dlls[0]) == "RatkinAnomalyMonolithFix.dll", "Duplicate or foreign runtime DLL");
    Console.WriteLine($"PASS: {version}; supplied Ratkin EndPlay/actor, original ending APIs, matching Harmony and loading folders.");
}
XElement ratkinDependency = metadata.Element("modDependencies").Elements("li")
    .Single(item => item.Element("packageId")?.Value == "fxz.ratkinanomaly.update");
Check(ratkinDependency.Element("steamWorkshopUrl")?.Value == "steam://url/CommunityFilePage/3293914637", "Wrong Ratkin Workshop link");
foreach (string language in new[] { "ChineseSimplified", "English", "French" })
{
    string file = Directory.GetFiles(Path.Combine(root, "Common/Languages", language, "Keyed"), "*.xml").Single();
    XElement data = XDocument.Load(file).Root;
    Check(data.Element("DisruptTheLinkFailedByRatkinAnomalyText") != null && data.Element("DisruptedLinkTextSuccessText") != null,
        "An existing ending translation was lost");
}
Console.WriteLine($"PASS: {count} dependency API, save-schema, loading and localization checks.");

sealed class AssemblyData : IDisposable
{
    static readonly SignatureNames signatureNames = new();
    readonly FileStream stream;
    readonly PEReader pe;
    readonly MetadataReader reader;
    readonly Dictionary<string, TypeDefinition> types;
    public string Name => reader.GetString(reader.GetAssemblyDefinition().Name);
    public Version Version => reader.GetAssemblyDefinition().Version;
    public AssemblyData(string path)
    {
        stream = File.OpenRead(path);
        pe = new PEReader(stream);
        reader = pe.GetMetadataReader();
        types = reader.TypeDefinitions.Select(reader.GetTypeDefinition).Where(type => !type.IsNested)
            .ToDictionary(type => reader.GetString(type.Namespace) + "." + reader.GetString(type.Name));
    }
    public bool HasType(string type) => types.ContainsKey(type);
    public bool Field(string type, string field, string expectedType) => types.TryGetValue(type, out var definition)
        && definition.GetFields().Select(reader.GetFieldDefinition).Any(member => reader.GetString(member.Name) == field
            && member.DecodeSignature(signatureNames, null) == expectedType);
    public bool Method(string type, string method, string[] parameters, string returns, bool isStatic)
    {
        if (!types.TryGetValue(type, out var definition)) return false;
        return definition.GetMethods().Select(reader.GetMethodDefinition).Any(member => {
            if (reader.GetString(member.Name) != method
                || member.Attributes.HasFlag(System.Reflection.MethodAttributes.Static) != isStatic) return false;
            MethodSignature<string> signature = member.DecodeSignature(signatureNames, null);
            return signature.ReturnType == returns
                && signature.ParameterTypes.SequenceEqual(parameters.Select(_ => "Verse.Pawn"))
                && member.GetParameters().Select(reader.GetParameter).Where(parameter => parameter.SequenceNumber > 0)
                    .Select(parameter => reader.GetString(parameter.Name)).SequenceEqual(parameters);
        });
    }
    public Dictionary<string, Version> References() => reader.AssemblyReferences.Select(reader.GetAssemblyReference)
        .ToDictionary(reference => reader.GetString(reference.Name), reference => reference.Version);
    public void Dispose() { pe.Dispose(); stream.Dispose(); }
}

sealed class SignatureNames : ISignatureTypeProvider<string, object>
{
    public string GetArrayType(string element, ArrayShape shape) => element + "[" + new string(',', shape.Rank - 1) + "]";
    public string GetByReferenceType(string element) => element + "&";
    public string GetFunctionPointerType(MethodSignature<string> signature) => "methodptr";
    public string GetGenericInstantiation(string type, ImmutableArray<string> arguments) => type + "<" + string.Join(",", arguments) + ">";
    public string GetGenericMethodParameter(object context, int index) => "!!" + index;
    public string GetGenericTypeParameter(object context, int index) => "!" + index;
    public string GetModifiedType(string modifier, string type, bool required) => type;
    public string GetPinnedType(string element) => element;
    public string GetPointerType(string element) => element + "*";
    public string GetPrimitiveType(PrimitiveTypeCode code) => "System." + code;
    public string GetSZArrayType(string element) => element + "[]";
    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte kind)
    {
        TypeDefinition type = reader.GetTypeDefinition(handle);
        return reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
    }
    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte kind)
    {
        TypeReference type = reader.GetTypeReference(handle);
        return reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
    }
    public string GetTypeFromSpecification(MetadataReader reader, object context, TypeSpecificationHandle handle, byte kind)
        => reader.GetTypeSpecification(handle).DecodeSignature(this, context);
}
