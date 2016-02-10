OneTap_MITBBS
=============

- Set up dev environment

1. Install Visual Studio 2012 or newer

2. Install Windows Phone 8.1 SDK

These semem to be all available through this link:

https://dev.windows.com/en-us/downloads/sdk-archive

- Start developing 

Open Projects\Apps\MitbbsReader8\MitbbsReader8.sln with Visual Studio

There are different variants of build target, but you should only need to care about Debug or Release, because we are going to remove free version and the China variant doesn't work in China any more.

Now choose 'Debug' as the target, build and run the app in Emulator. There may be some hardcoded dependency links in the project, which may not work on your machine. Please help to fix it to make it more generic if you get a chance.

You may need a Windows Developer account if you want to deploy it into your Windows Phone device through a USB cable.