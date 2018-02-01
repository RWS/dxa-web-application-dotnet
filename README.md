dxa-web-application-dotnet
===
SDL Digital Experience Accelerator ASP.NET MVC web application


About
-----
The SDL Digital Experience Accelerator (DXA) is a reference implementation of SDL Web (version 8, 8.5, or Cloud) that we provide to help you more quickly create, design, and publish a website based on SDL Web.

DXA is available for both .NET and Java web applications. Its modular architecture consists of a framework and example web application, which includes all core SDL Web functionality as well as separate Modules for additional, optional functionality.

This repository contains the source code of the DXA Framework and an example .NET web application. 

The full DXA distribution (including Content Manager-side items and installation support) is downloadable from the [SDL Community site](https://community.sdl.com/developers/tridion_developer/m/mediagallery/852) (latest version)
or the [Releases in GitHub](https://github.com/sdl/dxa-web-application-dotnet/releases) (all versions).
Furthermore, the DXA Framework is available on [NuGet.org](https://www.nuget.org/packages?q=dxa). 

To facilitate upgrades, we strongly recommend that you use official, compiled DXA artifacts from Maven Central instead of a custom build.
If you really must modify the DXA framework, we kindly request that you submit your changes as a Contribution (see the Branches and Contributions section below). 

Support
-------
At SDL we take your investment in Digital Experience very seriously, and will do our best to support you throughout this journey. 
If you encounter any issues with the Digital Experience Accelerator, please reach out to us via one of the following channels:

- Report issues directly in [this repository](https://github.com/sdl/dxa-web-application-dotnet/issues)
- Ask questions 24/7 on the SDL Web Community at https://tridion.stackexchange.com
- Contact Technical Support through the SDL Support web portal at https://www.sdl.com/support

Documentation
-------------
Documentation can be found online in the SDL documentation portal: http://docs.sdl.com/sdldxa20


Repositories
------------
The following repositories with source code are available:

 - https://github.com/sdl/dxa-content-management - CM-side framework (.NET Template Building Blocks)
 - https://github.com/sdl/dxa-html-design - Whitelabel HTML Design
 - https://github.com/sdl/dxa-model-service - Model Service (Java)
 - https://github.com/sdl/dxa-modules - Modules (.NET and Java)
 - https://github.com/sdl/dxa-web-application-dotnet - ASP.NET MVC web application (including framework)
 - https://github.com/sdl/dxa-web-application-java - Java Spring MVC web application (including framework)


Branches and Contributions
--------------------------
We are using the following branching strategy:

 - `master` - Represents the latest stable version. This may be a pre-release version (tagged as `DXA x.y Sprint z`). Updated each development Sprint (approximately bi-weekly).
 - `develop` - Represents the latest development version. Updated very frequently (typically nightly).
 - `release/x.y` - Represents the x.y Release. If hotfixes are applicable, they will be applied to the appropriate release branch so that the branch actually represents the initial release plus hotfixes.

All releases (including pre-releases and hotfix releases) are tagged. 

If you wish to submit a Pull Request, it should normally be submitted on the `develop` branch so that it can be incorporated in the upcoming release.

Fixes for severe/urgent issues (that qualify as hotfixes) should be submitted as Pull Requests on the appropriate release branch.

Always submit an issue for the problem, and indicate whether you think it qualifies as a hotfix. Pull Requests on release branches will only be accepted after agreement on the severity of the issue.
Furthermore, Pull Requests on release branches are expected to be extensively tested by the submitter.

Of course, it is also possible (and appreciated) to report an issue without associated Pull Requests.


License
-------
Copyright (c) 2014-2018 SDL Group.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and limitations under the License.
