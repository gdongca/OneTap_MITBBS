using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("OneTap MITBBS")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Charming CO2")]
[assembly: AssemblyProduct("OneTap Reader for MITBBS")]
[assembly: AssemblyCopyright("Copyright © Charming CO2 2014 (G. Dong)")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("3dbe164a-baef-4402-a93d-4ef70ca2e8c1")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("3.0.3.*")]
[assembly: AssemblyFileVersion("3.0.3.*")]
#if CHINA
[assembly: NeutralResourcesLanguageAttribute("zh-CN")]
#else
[assembly: NeutralResourcesLanguageAttribute("en-US")]
#endif
