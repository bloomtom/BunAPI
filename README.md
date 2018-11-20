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