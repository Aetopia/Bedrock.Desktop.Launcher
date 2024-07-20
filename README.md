# Bedrock Desktop Launcher
This is a fork of [Bedrock Updater](https://github.com/Aetopia/BedrockUpdater) which attempts to download, update & install Minecraft: Bedrock Edition as an unpackaged app.

## Process
- Download and install the Xbox Identity Provider.
- Download and install Minecraft: Bedrock Edition as an unpackaged app with developer mode enabled.

- Modify the `AppxManifest.xml` as follows:
    - Change `Name` to `Bedrock.Desktop.Release`.
    - Change `Company` to `CN=Bedrock.Desktop`
    - Remove `PackageDependency` of `Microsoft.Services.Store.Engagement`.
        - [This package is literally telemetry.](https://learn.microsoft.com/en-us/uwp/api/microsoft.services.store.engagement)

- This results in the installed to have the package family name of `Bedrock.Desktop.Release_svpbzhw13qwwr`. 

Doing this extremely useful for sideloading since:
- There is no need to re-register packages under the same package family name of `Microsoft.MinecraftUWP_cw5n1h2txyewy`.
- Each game version/instance gets their own app data folder since the package family name is different.
- Removes the need of symlinks or directory junctions when app data folders need to be switched.

## Building
1. Download the following:
    - [.NET SDK](https://dotnet.microsoft.com/en-us/download)
    - [.NET Framework 4.8.1 Developer Pack](https://dotnet.microsoft.com/en-us/download/dotnet-framework/thank-you/net481-developer-pack-offline-installer)

2. Run the following command to compile:

    ```cmd
    dotnet publish "src\BedrockUpdater.csproj"
    ```
