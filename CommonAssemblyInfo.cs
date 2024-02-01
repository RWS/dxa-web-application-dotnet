using System.Reflection;
using System.Runtime.InteropServices;

// Common assembly info shared by all projects/assemblies

#if DEBUG
    [assembly: AssemblyConfiguration("Debug")]
#else
    [assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCompany("RWS Group")]
[assembly: AssemblyProduct("RWS Digital Experience Accelerator")]

[assembly: ComVisible(false)]

// NOTE: Version Info and Copyright statement is automatically appended by the build process (ciBuild.proj)
[assembly: AssemblyVersion("0.0.0.0")]
[assembly: AssemblyFileVersion("0.0.0.0")]
[assembly: AssemblyCopyright("Copyright © 2014-2024 RWS Group")]
