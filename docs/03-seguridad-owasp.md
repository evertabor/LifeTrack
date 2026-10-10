# Seguridad — OWASP Top 10:2025 aplicado a LifeTrack

Este documento traduce cada categoría a **controles concretos**, **reglas para la IA** y **cómo se verifica**.
Estado: ☐ pendiente · ☑ hecho. Se actualiza al cerrar cada fase del plan.

> La IA genera código plausible, no necesariamente seguro. Toda salida de un agente se revisa con la checklist del final.

---

## A01 — Pérdida de control de acceso (*Broken Access Control*)
**Riesgo aquí:** ver o modificar datos de otro usuario cambiando un id (IDOR/BOLA); acceder a páginas sin sesión.

| Control | Dónde | Verificación |
|---|---|---|
| Política *fallback* = usuario autenticado (denegar por defecto) | `Program.cs` | Test de integración: páginas privadas redirigen sin sesión ☐ |
| Toda consulta filtra por `UserId` | Repositorios | Test de IDOR por handler ☐ |
| Recurso ajeno ⇒ `ResourceNotFoundException` (404 genérico, no 403) | Handlers | Tests de Application ☐ |
| `ICurrentUser` falla cerrado sin identidad | `Web/Services/CurrentUserService` | Test unitario ☐ |
| Ids no secuenciales (`Guid`) como defensa adicional, **no** como control principal | Domain | — |

**Reglas para la IA:** no crees repositorios con `GetById(id)` sin `userId`. No uses `[AllowAnonymous]` salvo en error, login/registro y `/health`. Cada handler con un id necesita su test de IDOR.

## A02 — Configuración de seguridad incorrecta (*Security Misconfiguration*)
| Control | Dónde | Verificación |
|---|---|---|
| Secretos fuera del repo (`user-secrets`, App Settings) | Config | Escaneo de secretos en CI ☐ |
| Clave de licencia de MediatR solo en producción y solo por configuración; su ausencia no rompe la app | Config, Azure | Revisión de despliegue ☐ |
| Cabeceras: CSP, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, COOP | `Security/` | Test de integración que comprueba las cabeceras ☐ |
| HTTPS obligatorio: `UseHttpsRedirection`, `UseHsts`, "HTTPS Only" en App Service | `Program.cs`, Azure | Revisión de despliegue ☐ |
| Página de error genérica; sin detalles en producción | `Program.cs` | Revisión manual ☐ |
| `AllowedHosts` restringido al dominio real | Config | Revisión de despliegue ☐ |
| Migraciones automáticas solo en Development | `Program.cs` | Revisión de código ☐ |
| Puertos de SQL local solo en `127.0.0.1` | `docker-compose.yml` | Revisión ☐ |
| Reglas de firewall de Azure SQL mínimas | Azure | Checklist de despliegue ☐ |

**Reglas para la IA:** nunca pongas cadenas de conexión, contraseñas ni claves en código ni en `appsettings.json`. Esto incluye `MediatR:LicenseKey`. No habilites `DeveloperExceptionPage` fuera de Development. Si necesitas relajar la CSP, pide aprobación y documenta por qué.

## A03 — Fallos en la cadena de suministro de software (*Software Supply Chain Failures*)
| Control | Dónde | Verificación |
|---|---|---|
| Versiones solo en `Directory.Packages.props` (CPM) | Raíz | Revisión de PR ☐ |
| `MediatR` **aprobado y pineado** (`13.*`), solo en `LifeTrack.Application` y `LifeTrack.Web` | `Directory.Packages.props` | Revisión de PR ☐ |
| Clave de licencia de MediatR (`MediatR:LicenseKey`) como **App Setting secreto**, nunca en el repositorio ni en `appsettings.json` | Azure, `dotnet user-secrets` en local | Escaneo de secretos en CI ☐ |
| `NuGetAudit` activo; `dotnet list package --vulnerable --include-transitive` sin hallazgos | CI | Falla el pipeline ☐ |
| `packages.lock.json` y `restore --locked-mode` tras el primer restore | CI | ☐ |
| Dependabot para NuGet y GitHub Actions | `.github/dependabot.yml` | ☐ |
| CodeQL para C# | CI | ☐ |
| Acciones de GitHub fijadas por versión (idealmente por SHA) y permisos mínimos del workflow | CI | ☐ |
| Despliegue por OIDC (sin credenciales de larga duración) | CI/CD | ☐ |

**Coste en licencia de MediatR.** La versión **13.0.0 (julio de 2025)** dejó de distribuirse bajo
Apache 2.0 y es **dual: RPL-1.5 o licencia comercial**. Las versiones **v12.5.0 y anteriores**
siguen bajo Apache 2.0, pero **sin soporte, sin actualizaciones de seguridad y sin parches**, así que
no son una alternativa mantenible. v13 **exige aceptar la licencia** al instalar. Para uso
**personal, educativo y no comercial** —el caso de LifeTrack— existe la **Community License**,
gratuita y de **autoservicio**, sin proceso de aprobación.

**Dónde va la clave.** **No hace falta clave** en desarrollo, CI/CD, staging, QA ni integración:
**solo en despliegues de producción**. Se pasa por configuración (variable de entorno, App Settings
como secret o secret store) y **nunca** hardcodeada. Sin clave, con clave expirada o inválida, la
librería **solo emite warnings o errores en el log**: la ejecución de la licencia es autocontenida,
sin servidor de licencias y **sin llamadas de red**, y no hay límites de ejecución, ni bloqueo de
funcionalidad, ni degradación.

**Reglas para la IA:** **no añadas paquetes** sin aprobación. Los modelos inventan nombres de paquetes o recomiendan versiones obsoletas: verifica que el paquete existe, su autor, su mantenimiento y su licencia. Prefiere la BCL y lo que ya trae ASP.NET Core. No añadas versiones de `MediatR` por tu cuenta (la fija el CPM) ni escribas una clave de licencia en ningún fichero del repositorio.

## A04 — Fallos criptográficos (*Cryptographic Failures*)
| Control | Dónde |
|---|---|
| Contraseñas con el hasher de Identity (PBKDF2 con sal); nunca propio | Identity |
| TLS en tránsito (HTTPS y `Encrypt=True` en la cadena de conexión) | Web, BD |
| Cifrado en reposo de Azure SQL (TDE, activo por defecto) | Azure |
| Cookies `Secure`, `HttpOnly`, `SameSite`; Data Protection de ASP.NET Core para tokens | Web |
| Analizadores CA5350/5351/5358/5379/5394 como *warning* (error en CI) | `.editorconfig` |

**Reglas para la IA:** prohibido MD5/SHA1/DES/3DES, `Random` para tokens, claves hardcodeadas y cifrado casero. Para aleatoriedad de seguridad usa `RandomNumberGenerator`.

## A05 — Inyección (*Injection*)
| Control | Dónde | Verificación |
|---|---|---|
| EF Core + LINQ (consultas parametrizadas) | Repositorios | Revisión + CA2100/CA3001 ☐ |
| SQL crudo solo con `FromSql($"…")` interpolado; nunca concatenado | Infrastructure | Búsqueda de `FromSqlRaw` en revisión ☐ |
| Codificación de salida de Razor; sin `Html.Raw` con datos de usuario | Vistas | Búsqueda en revisión/CI ☐ |
| CSP sin scripts en línea | `Security/` | Test de cabeceras ☐ |
| Validación de forma (DataAnnotations) + de negocio (invariantes del dominio) | Web, Domain | Tests ☐ |
| Importación CSV (RF-F07): validar tamaño, filas, tipos y **escapar fórmulas** (`=`, `+`, `-`, `@`) al exportar | Futuro | Tests ☐ |

**Reglas para la IA:** no construyas SQL con `string.Format`/interpolación en `FromSqlRaw`/`ExecuteSqlRaw`. No uses `Html.Raw`. No confíes en validación solo del cliente.

## A06 — Diseño inseguro (*Insecure Design*)
| Control | Dónde |
|---|---|
| Modelo de amenazas (STRIDE) en la especificación | `docs/01` |
| Invariantes en el dominio (monto, fechas, unicidad) | Domain |
| Limitador de tasa global | `Program.cs` |
| `InputModel` dedicados (sin *over-posting*) | Web |
| Mínimos datos personales; borrado en cascada de la cuenta | Domain/Infra |
| Límites de tamaño en entradas (longitudes máximas, tamaño de CSV) | Web/Domain |

**Reglas para la IA:** ante una funcionalidad nueva, escribe primero el abuso posible ("¿qué pasa si un usuario envía X repetido, enorme o ajeno?") y cúbrelo con un test.

## A07 — Fallos de autenticación (*Authentication Failures*)
| Control | Dónde |
|---|---|
| Identity con contraseña ≥ 12 caracteres | `Program.cs` |
| Bloqueo tras 5 intentos fallidos durante 15 min | `Program.cs` |
| Mensajes de login genéricos (no revelan si el correo existe) | Identity UI |
| Cookie de sesión con expiración (8 h, deslizante) | `Program.cs` |
| Confirmación de correo y MFA | Pendiente (RF-A05) ☐ |
| Registro abierto deshabilitable tras crear la cuenta personal | Pendiente (endurecimiento) ☐ |

**Reglas para la IA:** no implementes autenticación ni gestión de contraseñas a mano; usa Identity.

## A08 — Fallos de integridad de software o datos (*Software or Data Integrity Failures*)
| Control | Dónde |
|---|---|
| Anti-forgery en todo POST vía filtro global `AutoValidateAntiforgeryToken` + tag Helpers | Web |
| Sin deserialización insegura (`BinaryFormatter` prohibido: CA2300/2301) | `.editorconfig` |
| Pipeline con permisos mínimos y OIDC | CI/CD |
| Migraciones revisadas y aplicadas desde el pipeline, no ad-hoc | CI/CD |
| Restricción de restauración con *lock file* | CI |

**Reglas para la IA:** no uses `[IgnoreAntiforgeryToken]` ni `[ValidateAntiForgeryToken(false)]`. No descargues ni ejecutes scripts remotos en el pipeline sin fijar su versión.

## A09 — Fallos en el registro y las alertas (*Security Logging and Alerting Failures*)
| Control | Dónde |
|---|---|
| `ILogger` con plantillas estructuradas | Web, Infra |
| Se registran: ids manipulados (`ResourceNotFoundException`), bloqueos de cuenta, errores 5xx | Web |
| **No** se registran: contraseñas, tokens, cadenas de conexión, correos innecesarios | Todo |
| Identificador de solicitud en la página de error para correlacionar | Web |
| Alertas: Sentry o Azure Monitor/Application Insights (opcional) | Operación ☐ |

**Reglas para la IA:** usa `LogWarning("Habit {HabitId} not found", id)`, **nunca** `LogWarning($"Habit {id} …")`. Sin datos personales en logs.

## A10 — Mal manejo de condiciones excepcionales (*Mishandling of Exceptional Conditions*)
| Control | Dónde |
|---|---|
| Fallar cerrado: sin identidad no hay datos (`CurrentUser` lanza) | Web |
| Excepciones tipadas y traducción en la frontera: el dominio lanza `DomainException`/`NotFoundException` y el handler las traduce a `BusinessRuleException`/`ResourceNotFoundException`; manejador global para el resto | Todo |
| Sin `catch` vacíos ni `catch (Exception)` que silencien | Todo |
| `CancellationToken` en todo I/O; sin `.Result`/`.Wait()` | Todo |
| Reintentos solo ante errores transitorios de BD (`EnableRetryOnFailure`) | Infra |
| Transacción por caso de uso (un único `SaveChangesAsync`) | Application |

**Reglas para la IA:** no devuelvas ni muestres `ex.Message` de excepciones desconocidas. No conviertas errores en valores por defecto silenciosos.

---

## Si añades IA al producto — OWASP Top 10 para aplicaciones LLM
Aplica al extra RF-X02 (categorizar gastos) o a cualquier función con LLM.

| Riesgo | Control |
|---|---|
| **Inyección de prompt** | Separar instrucciones de datos del usuario; tratar la descripción del gasto como dato no confiable; no ejecutar acciones a partir de la salida |
| **Manejo inseguro de la salida** | Validar la respuesta contra una lista cerrada de categorías del usuario; codificar al mostrar; nunca `Html.Raw` |
| **Agencia excesiva** | El modelo solo **sugiere**; el usuario confirma. Sin herramientas con efectos secundarios |
| **Divulgación de información sensible** | Enviar solo la descripción mínima necesaria; nunca montos acumulados, correos ni ids |
| **Consumo no acotado** | Límite de tasa y de tamaño de entrada; presupuesto de llamadas (la capa gratuita de GitHub Models tiene límites) |
| **Dependencia del modelo** | Respaldo determinista por palabras clave si el modelo falla o no responde |

---

## Checklist de revisión de PR (humano o IA)
- [ ] ¿Toda consulta nueva filtra por `UserId`? ¿Hay test de IDOR para el handler?
- [ ] ¿Algún endpoint o página nueva queda accesible sin sesión?
- [ ] ¿Se enlazan formularios a un `InputModel` (no a entidades)?
- [ ] ¿Hay `Html.Raw`, SQL concatenado, `FromSqlRaw` con variables o scripts en línea?
- [ ] ¿Se añadió algún paquete NuGet? ¿Fue aprobado y verificado?
- [ ] ¿Aparece alguna clave de licencia (incluida `MediatR:LicenseKey`) en el diff?
- [ ] ¿Hay secretos, cadenas de conexión o datos reales en el diff?
- [ ] ¿Los logs usan plantillas y evitan datos personales?
- [ ] ¿Los errores desconocidos se manejan sin exponer detalles?
- [ ] ¿Se mantuvieron intactos CSP, anti-forgery, autorización *fallback* y tests de arquitectura?
- [ ] ¿`dotnet list package --vulnerable` está limpio?
