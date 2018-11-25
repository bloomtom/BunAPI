# BunAPI
>A C# library for interfacing with [BunnyCDN](https://bunnycdn.com/) cloud storage.

This library is a straightforward wrapper around the BunnyCDN file storage REST API.


## Nuget Packages

Package Name | Target Framework | Version
---|---|---
[BunAPI](https://www.nuget.org/packages/bloomtom.BunAPI) | .NET Standard 2.0 | ![NuGet](https://img.shields.io/nuget/v/bloomtom.BunAPI.svg)


## Usage
Create a new instance of `BunAPI.BunClient` using your API key and a "storage zone". Storage zones can be created [in your account dashboard](https://bunnycdn.com/dashboard/storagezones). Once created, your client can be used to list, upload, download and delete files.

#### Finding Your API Key
Your API key can be found in your account under "Storage". Open a connection zone, then go to "FTP & API Access". In the online interface the API key is called a password.

## Testing
To run tests, you'll need to add a file to the project containing test configuration. The file should be `ConnectionInfo.cs` and should be stored under the BunTests directory. The file should contain one class with the following structure:
```csharp
namespace BunTests
{
    internal static class ConnectionInfo
    {
        public static readonly string zone = "yourTestZone";
        public static readonly string apiKey = "yourTestApiKey";
        public static readonly string badZone = "thisshouldntexist.donotcreate";
        public static readonly string badKey = "notarealkey";
    }
}
```
The variables `zone` and `apiKey` should be filled in with your testing zone and key respectively. The `badZone` and `badKey` variables can be set to whatever you want as long as they aren't valid.
