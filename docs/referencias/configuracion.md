# Parte A — Configuración de la solución

## global.json (fija el SDK)

`global.json`

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

## Directory.Build.props (warnings como error solo en CI)

Los warnings son errores **solo en CI**: en local solo avisan, para que corregir el estilo no bloquee
al que está construyendo. La condición `'$(CI)' == 'true'` la activa el entorno de GitHub Actions, y
`ContinuousIntegrationBuild` hace que las rutas embebidas sean reproducibles.

`Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <Deterministic>true</Deterministic>
    <TreatWarningsAsErrors Condition="'$(CI)' == 'true'">true</TreatWarningsAsErrors>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>
</Project>
```

## Directory.Packages.props (versiones centralizadas, auditoría NuGet)

Este fichero es el **único** sitio donde viven las versiones: los `.csproj` referencian sin versión y
el CPM resuelve aquí. Centralizarlo es lo que hace posible la auditoría de NuGet, porque
`NuGetAudit` con nivel `low` evalúa el grafo transitivo completo desde una sola lista.

`MediatR` va pineada en `13.*` y es **dual** (RPL-1.5 o licencia comercial): la Community License
gratuita cubre uso personal, educativo y no comercial, que es el caso de este proyecto. El resto de
versiones flotan en la versión mayor porque son paquetes de Microsoft con soporte junto al runtime.

`Directory.Packages.props`

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageFloatingVersionsEnabled>true</CentralPackageFloatingVersionsEnabled>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>all</NuGetAuditMode>
    <NuGetAuditLevel>low</NuGetAuditLevel>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.*" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.*" />
    <PackageVersion Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.*" />
    <PackageVersion Include="Microsoft.AspNetCore.Identity.UI" Version="10.0.*" />
    <PackageVersion Include="MediatR" Version="13.*" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageVersion Include="xunit" Version="2.9.*" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.*" />
    <PackageVersion Include="coverlet.collector" Version="6.*" />
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.*" />
  </ItemGroup>
</Project>
```

## .editorconfig (estilo + analizadores de seguridad)

Los analizadores de OWASP se activan por `dotnet_diagnostic.<id>.severity`, y los comentarios `#` de
este fichero los agrupan por familia (inyección, criptografía, redirecciones) porque cada identificador
por separado no dice qué amenaza cubre. Aquí los comentarios `#` sí se mantienen: `.editorconfig` no es
código y `#` es el único canal de documentación del formato.

`.editorconfig`

```ini
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.{csproj,props,targets,json,yml,yaml,md}]
indent_size = 2

[*.cs]
csharp_style_namespace_declarations = file_scoped:warning
csharp_using_directive_placement = outside_namespace:warning
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_prefer_braces = true:suggestion
dotnet_style_qualification_for_field = false:suggestion
dotnet_sort_system_directives_first = true

# ---- Security analyzers: always on (OWASP A05 Injection, A04 Cryptographic Failures) ----
# CA2100: SQL built from strings. CA3001: SQL injection (taint). CA3002: XSS (taint).
# CA3003: file path injection. CA3006: process command injection. CA3007: open redirect.
# CA5350/CA5351: weak or broken crypto (SHA1, 3DES, MD5, DES). CA5358: insecure cipher modes.
# CA5379: weak key derivation. CA5394: insecure randomness. CA2300/CA2301: insecure deserialization.
dotnet_diagnostic.CA2100.severity = warning
dotnet_diagnostic.CA3001.severity = warning
dotnet_diagnostic.CA3002.severity = warning
dotnet_diagnostic.CA3003.severity = warning
dotnet_diagnostic.CA3006.severity = warning
dotnet_diagnostic.CA3007.severity = warning
dotnet_diagnostic.CA5350.severity = warning
dotnet_diagnostic.CA5351.severity = warning
dotnet_diagnostic.CA5358.severity = warning
dotnet_diagnostic.CA5379.severity = warning
dotnet_diagnostic.CA5394.severity = warning
dotnet_diagnostic.CA2300.severity = warning
dotnet_diagnostic.CA2301.severity = warning

# ---- Not useful in ASP.NET Core (ConfigureAwait) ----
dotnet_diagnostic.CA2007.severity = none

# Underscores in test names are fine.
[tests/**.cs]
dotnet_diagnostic.CA1707.severity = none
```

## docker-compose.yml (SQL Server solo para desarrollo local)

`docker-compose.yml`

```yaml
# Local SQL Server for development only.
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: ${MSSQL_SA_PASSWORD:?Define MSSQL_SA_PASSWORD in .env}
    ports:
      - "127.0.0.1:1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
volumes:
  sqldata: {}
```

## .gitignore

`.gitignore`

```gitignore
bin/
obj/
.vs/
.idea/
*.user
*.suo
TestResults/
coverage*/
*.log
# Secrets and local configuration: NEVER commit
.env
appsettings.*.local.json
secrets.json
*.pfx
*.publish.xml
publish/
```

## .env.example (plantilla; el `.env` real no se sube)

`.env.example`

```
# Copia este archivo a .env (ignorado por git) y define una contraseña fuerte.
# Se usa únicamente por docker-compose para el SQL Server de desarrollo local.
MSSQL_SA_PASSWORD=
```

