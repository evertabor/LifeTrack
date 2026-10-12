# Despliegue en Azure (capa gratuita)

> Los límites y los nombres de comandos cambian. Antes de ejecutar cada comando revisa `--help` y la oferta vigente; en el portal, comprueba que el resumen de creación muestre costo **0**.

## 1. Recursos

| Recurso | SKU / oferta | Notas |
|---|---|---|
| Grupo de recursos | — | Agrupa todo para poder borrarlo de una vez |
| App Service Plan | **F1** (gratuito) | Cuota diaria de CPU limitada, sin *always-on*, sin SLA |
| Web App | .NET 10 (Linux preferido; Windows si F1 Linux no está disponible en la región) | Activar "solo HTTPS" |
| Azure SQL Database | **Free offer** (serverless) | Límites mensuales de vCore-segundos y almacenamiento; se pausa por inactividad |

Si tu suscripción es *Azure for Students Starter*, la oferta gratuita de Azure SQL puede no ser compatible: revisa el tipo de suscripción.

## 2. Aprovisionamiento (referencia con Azure CLI)

```bash
az group create -n rg-lifetrack -l <region>

az appservice plan create -g rg-lifetrack -n plan-lifetrack --sku F1 --is-linux
az webapp create -g rg-lifetrack -p plan-lifetrack -n <app-unica> --runtime "DOTNETCORE:10.0"
az webapp update -g rg-lifetrack -n <app-unica> --https-only true

az sql server create -g rg-lifetrack -n <sql-unico> -l <region> -u <admin> -p '<contraseña-larga>'
az sql db create -g rg-lifetrack -s <sql-unico> -n LifeTrack \
   --use-free-limit --free-limit-exhaustion-behavior AutoPause
```
Verifica: `az webapp list-runtimes --os linux` (cadena exacta del runtime) y `az sql db create --help` (parámetros de la oferta gratuita). Si F1 Linux no está disponible en tu región, prueba otra o usa Windows.

### Firewall de Azure SQL
La regla "Permitir servicios de Azure" (0.0.0.0) es cómoda pero **amplia**: permite a cualquier recurso de Azure intentar conectar. Mitigaciones: contraseña larga y única, usuario sin privilegios para la app (no el administrador) y, si es posible, reglas con las IP salientes de la web app. Evalúa identidad administrada de Azure como mejora posterior.

## 3. Configuración de la aplicación (App Settings)
No se suben al repositorio. Todas se declaran como App Settings **secretos** en el App Service.

| Clave | Valor |
|---|---|
| `ConnectionStrings__DefaultConnection` | Cadena de Azure SQL con `Encrypt=True` (en la pestaña **Cadenas de conexión** como `DefaultConnection` de tipo `SQLAzure`) |
| `MediatR__LicenseKey` | Clave de licencia de MediatR, **solo en producción**. App Setting **secreto** |
| `App__TimeZoneId` | Zona IANA del usuario (p. ej. `America/Bogota`) — **obligatoria** para que "hoy" sea correcto |
| `AllowedHosts` | `<app-unica>.azurewebsites.net` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

`MediatR__LicenseKey` se declara como App Setting **marcado como secreto** ( Azure Portal → Configuration
→ Mark as secret), nunca como texto plano ni en el repositorio. **No es obligatoria:** si falta, la app
funciona con normalidad y MediatR solo deja un *warning* en el log. Su ejecución de licencia es
autocontenida, sin servidor de licencias ni llamadas de red: no hay límites de ejecución ni bloqueo de
funcionalidad. El doble guion `__` es el separador de anidamiento de configuración de .NET, por eso la
clave aparece en la tabla como `MediatR__LicenseKey` y se lee en código como `MediatR:LicenseKey`.

El usuario de base de datos de la aplicación debería tener solo permisos de lectura/escritura sobre su esquema; las migraciones se aplican con un usuario/paso distinto.

## 4. Migraciones en producción
**No** se aplican al arrancar. Opciones (de más a menos recomendable):
1. En el pipeline: generar un script idempotente (`dotnet ef migrations script --idempotent`) y ejecutarlo contra Azure SQL con credenciales propias del pipeline.
2. Un *bundle* (`dotnet ef migrations bundle`) ejecutado por el pipeline.
3. Manualmente desde tu equipo, abriendo temporalmente tu IP en el firewall (ciérrala después).

## 5. CI/CD con GitHub Actions (plantillas)

**`.github/workflows/ci.yml`**
```yaml
name: CI
on:
  push: { branches: [main] }
  pull_request:
permissions:
  contents: read
jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@11d5960a326750d5838078e36cf38b85af677262 # v4
      - uses: actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9 # v4
        with: { global-json-file: global.json }
      - run: dotnet restore            # usa --locked-mode cuando exista packages.lock.json
      - run: dotnet format --verify-no-changes --no-restore
      - run: dotnet build -c Release --no-restore
      - run: dotnet test -c Release --no-build --collect:"XPlat Code Coverage"
      - name: Paquetes vulnerables
        run: |
          dotnet list package --vulnerable --include-transitive 2>&1 | tee vuln.log
          ! grep -q "has the following vulnerable packages" vuln.log
```
(GitHub define `CI=true` automáticamente, así que los warnings son errores.)

**`.github/workflows/deploy.yml`** (despliegue con OIDC, sin secretos de larga duración)
```yaml
name: Deploy
on:
  workflow_dispatch:
  push: { branches: [main] }
permissions:
  id-token: write
  contents: read
jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@11d5960a326750d5838078e36cf38b85af677262 # v4
      - uses: actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9 # v4
        with: { global-json-file: global.json }
      - run: dotnet restore --locked-mode
      - run: dotnet build -c Release --no-restore
      - name: Generar script de migraciones
        env:
          ConnectionStrings__DefaultConnection: Server=tcp:placeholder.database.windows.net,1433;Initial Catalog=LifeTrack;User ID=placeholder;Password=placeholder;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
        run: |
          dotnet tool restore
          dotnet tool run dotnet-ef migrations script --idempotent --project src/LifeTrack.Infrastructure --startup-project src/LifeTrack.Web --output migrations.sql --no-build
      - name: Aplicar migraciones
        uses: Azure/sql-action@dae62750f3e645283b7755717b44a2029c0735fb # v2.3
        with:
          connection-string: ${{ secrets.AZURE_SQL_MIGRATION_CONNECTION_STRING }}
          path: ./migrations.sql
      - run: dotnet publish src/LifeTrack.Web -c Release --no-build -o publish
      - uses: azure/login@8216e11d8cd9b42fe925c852af8e76311ff067ac # v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      - uses: azure/webapps-deploy@b686016b4de8820a23519503685d8d03fbfa03a6 # v3
        with:
          app-name: lifetrack
          package: publish
```
El paso de migraciones usa una cadena de marcador solo para generar el script (nunca conecta); la
aplicación real usa el secreto `AZURE_SQL_MIGRATION_CONNECTION_STRING`, con un usuario de migración
separado de `lifetrack_app`. La herramienta `dotnet-ef` se fija en `dotnet-tools.json` (EF 10).
Pasos únicos para OIDC: crear una aplicación en Microsoft Entra, añadir una **credencial federada** para tu repositorio (rama `main` o el entorno `production`), darle rol *Contributor* solo sobre el grupo de recursos y guardar `client-id`, `tenant-id` y `subscription-id` como secretos del repositorio. Consulta la guía oficial de "Deploy to App Service using GitHub Actions" para los comandos vigentes.

También: **CodeQL** (plantilla de GitHub para C#) y **`dependabot.yml`** para `nuget` y `github-actions`.

## 6. Límites a tener presentes
- F1: cuota diaria de minutos de CPU. Al agotarla, la app deja de responder hasta el reinicio de la cuota.
- Sin *always-on*: la primera visita tras inactividad tarda más.
- Azure SQL free: se pausa si no se usa; la primera consulta tarda mientras reanuda (`EnableRetryOnFailure` lo absorbe).
- Sin SLA ni dominio propio con certificado en F1.
- **Control de costos:** crea una alerta de presupuesto en Azure Cost Management y comprueba periódicamente que no haya recursos de pago.

## 7. Checklist posterior al despliegue
- [ ] HTTPS obligatorio activo y `AllowedHosts` configurado con el dominio real (sin esto, el valor local `localhost` devuelve 400 en el dominio real).
- [ ] `App__TimeZoneId` definida.
- [ ] `MediatR__LicenseKey` declarada como secreto **solo en producción** (opcional: sin ella la app funciona y solo queda un *warning* en el log).
- [ ] Cabeceras de seguridad presentes (revísalas con las herramientas del navegador).
- [ ] La página de error no muestra trazas.
- [ ] Registro funcionando; las páginas privadas redirigen sin sesión; `/health` responde.
- [ ] Sin secretos en el repositorio ni en los logs del pipeline.
- [ ] Alerta de presupuesto creada.
