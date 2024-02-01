RWS Digital Experience Accelerator ASP.NET MVC web application
===
Build status
------------
- Develop: ![Build Status](https://github.com/rws/dxa-web-application-dotnet/workflows/Build/badge.svg?branch=develop)
- 1.8: ![Build Status](https://github.com/rws/dxa-web-application-dotnet/workflows/Build/badge.svg?branch=release/1.8)

Prerequisites
-------------
For building .NET repositories you must have the following installed:
- Visual Studio 2019
- .NET Framework 4.8

Build
-----
```
msbuild ciBuild.proj /t:Restore
msbuild ciBuild.proj
msbuild ciBuild.proj /t:Artifacts
```

About
-----
The RWS Digital Experience Accelerator (DXA) is a reference implementation of RWS Tridion Sites 9 and RW Web 8 intended to help you create, design and publish an RWS Tridion/Web-based website quickly.

DXA is available for both .NET and Java web applications. Its modular architecture consists of a framework and example web application, which includes all core RWS Tridion/Web functionality as well as separate Modules for additional, optional functionality.

This repository contains the source code of the DXA Framework and an example .NET web application. 

The full DXA distribution (including Content Manager-side items and installation support) is downloadable from the [RWS AppStore](https://appstore.rws.com/?q=dxa) 
or the [Releases in GitHub](https://github.com/rws/dxa-web-application-dotnet/releases).
Furthermore, the DXA Framework is available on [NuGet.org](https://www.nuget.org/packages?q=dxa). 

To facilitate upgrades, we strongly recommend that you use official, compiled DXA artifacts from Maven Central instead of a custom build.
If you really must modify the DXA framework, we kindly request that you submit your changes as a Contribution (see the Branches and Contributions section below). 

Support
-------
At RWS we take your investment in Digital Experience very seriously, if you encounter any issues with the Digital Experience Accelerator, please use one of the following channels:

- Report issues directly in [this repository](https://github.com/rws/dxa-web-application-dotnet/issues)
- Ask questions 24/7 on the RWS Tridion Community at https://tridion.stackexchange.com
- Contact RWS Professional Services for DXA release management support packages to accelerate your support requirements

Documentation
-------------
Documentation can be found online in the RWS documentation portal: https://docs.rws.com/sdldxa


Repositories
------------
You can find all the DXA related repositories [here](https://github.com/rws/?q=dxa&type=source&language=)

Branches and Contributions
--------------------------
We are using the following branching strategy:

 - `develop` - Represents the latest development version.
 - `release/x.y` - Represents the x.y Release. If hotfixes are applicable, they will be applied to the appropriate release branch so that the branch actually represents the initial release plus hotfixes.

All releases (including pre-releases and hotfix releases) are tagged. 

If you wish to submit a Pull Request, it should normally be submitted on the `develop` branch so that it can be incorporated in the upcoming release.

Fixes for severe/urgent issues (that qualify as hotfixes) should be submitted as Pull Requests on the appropriate release branch.

Always submit an issue for the problem, and indicate whether you think it qualifies as a hotfix. Pull Requests on release branches will only be accepted after agreement on the severity of the issue.
Furthermore, Pull Requests on release branches are expected to be extensively tested by the submitter.

Of course, it is also possible (and appreciated) to report an issue without associated Pull Requests.


License
-------
Copyright (c) 2014-2024 RWS Group.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and limitations under the License.
