# Development Notes

Notes, todo, plan, backlog, etc

## Overview

Improve load certificate, test TLS authentication, naming CertificateInfo configuration

## Todo

- Drop support NETCore App 3.1

## Backlog

- TBD

## Changes Log

### v2.2.0
 - Drop support .NET 5.0
 - Decoupling to NetLah.Abstractions, use NetLah.AssemblyInfo.BuildTime.Target
 - Add .NET 7.0
 - Add more Create APIs
 - Improve nullable
 - More test on json configuration provider (such as appsettings.json, appsettings.Production.json, secrets.json, appsettings.ini)
 - More test on enum binder (flags), support both interger and multi flags Flag1,Flag2
 - Test parsing enum, both interger and multi flags Flag1,Flag2
 - Test Authentication TLS
 - Fix Authentication TLS on Windows: support reimport and change to default Exportable | EphemeralKeySet
 - New configuration: CertificateConfig with key storage flags and reimport
