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

 ### Generate Self-Signed Certificate
 -  `-TextExtension @("2.5.29.19={critical} {text}CA=false")`

 - New-SelfSignedCertificate -CertStoreLocation 'Cert:\CurrentUser\My' -NotAfter (Get-Date).AddYears(50) -Subject 'development.dummy_ecdsa_p384-2024Nov' -FriendlyName 'development.dummy_ecdsa_p384-2024Nov' -KeyAlgorithm ECDSA_P384 -HashAlgorithm SHA384 -KeyUsage DigitalSignature,NonRepudiation -CurveExport CurveName
 
 - New-SelfSignedCertificate -CertStoreLocation 'Cert:\CurrentUser\My' -NotAfter (Get-Date).AddYears(50) -Subject 'development.dummy_ecdsa_p521-2024Nov' -FriendlyName 'development.dummy_ecdsa_p521-2024Nov' -KeyAlgorithm ECDSA_P521 -HashAlgorithm SHA384 -KeyUsage DigitalSignature,NonRepudiation -CurveExport CurveName
