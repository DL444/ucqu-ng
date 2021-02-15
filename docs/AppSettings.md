# UCQU 后端应用设置
本文档介绍 UCQU 后端运行所需的应用设置。

## 应用设置
应用设置是为 Azure 函数计算应用配置环境变量的方式。你可以在 Azure 管理门户中相关资源的 Configuration 页面，或使用 Azure 的两种命令行工具设置这些参数。

在本地开发与调试时，请在后端项目 (即 `DL444.Ucqu.Backend` 项目) 目录下创建 `local.settings.json` 文件。该文件将在本地环境下提供应用设置。注意该文件将包含机要信息，因此应当将该文件从版本控制系统中排除。此外，本文件应当仅在本地开发环境使用，因此不要将该文件部署至生产环境。该文件的格式与用法请参见[文档](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#local-settings-file)。

应用设置键中的 `:` 代表层级关系。但 Linux 环境下部署的函数应用不支持键中含有 `:`. 此时，请将 `:` 替换为 `__`.

## Key Vault 引用
[Key Vault](https://docs.microsoft.com/en-us/azure/key-vault/general/overview) 是 Azure 提供的机要管理解决方案。使用 Key Vault 时，密钥等机要数据可以不直接存储在代码或应用设置中。应用程序通过 Azure AD 进行身份验证与鉴权，进而从 Key Vault 中获取机要数据。

[Key Vault 引用](https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references)是一种简单的 Key Vault 集成方案。开发人员只需将直接存储在应用设置中的机要更改为相应的 Key Vault 引用字符串即可，而无需修改代码。因此，本文档中提及的所有应用设置值均可使用 Key Vault 引用代替。

Azure 应用设置是加密存储的，因此将机要直接存放在应用设置中也是安全的。但 Key Vault 可以提供更强大的权限与版本管理功能。本项目提供的部署模板将把机要数据存储在 Key Vault 中，并使用 Key Vault 引用来读取机要。

## 应用设置项
应用程序运行所需的设置项及相应的数据类型如下。

数据类型后附加 `?` 的代表本项可选。请配置好全部的必须设置项，否则应用程序将无法正常工作。注意有些可选的设置项，在某些配置状态下是必须的，请参见详细说明。
```json
{
    "AzureWebJobsStorage": "string",
    "FUNCTIONS_WORKER_RUNTIME": "string",
    "FUNCTIONS_EXTENSION_VERSION": "string",
    "APPINSIGHTS_INSTRUMENTATIONKEY": "string?",
    "AzureWebJobsDashboard": "string?",
    "ClientAuthentication:Enabled": "bool?",
    "ClientAuthentication:DisableChainValidation": "bool?",
    "ClientAuthentication:CertificateReference": "string?",
    "WEBSITE_LOAD_USER_PROFILE": "int?",
    "Credential:EncryptionKey": "string",
    "Database:ConnectionString": "string",
    "Database:Database": "string",
    "Database:Container": "string",
    "EventPublish:TopicUri": "string",
    "EventPublish:TopicKey": "string",
    "Host:ServiceBaseAddress": "string",
    "Notification:MaxChannelCountPerPlatform": "int?",
    "Notification:Retry": "int?",
    "Notification:Windows:PackageSid": "string?",
    "Notification:Windows:Secret": "string?",
    "ScoreRefreshTimer": "string",
    "Term:CurrentTerm": "string",
    "Term:TermStartDate": "string",
    "Term:TermEndDate": "string",
    "Token:Issuer": "string",
    "Token:ValidMinutes": "int?",
    "Token:SigningKey": "string",
    "Upstream:Host": "string",
    "Upstream:UseTls": "bool?",
    "Upstream:Timeout": "int?"
}
```

### 平台依赖
这些设置项是 Azure 函数计算平台的底层依赖设置。
- `AzureWebJobsStorage`  
    一个存储帐户的连接字符串。  
    Azure 函数计算平台中的许多功能都依赖于存储帐户。不同的函数计算应用应当分别使用单独的存储帐户。
- `FUNCTIONS_WORKER_RUNTIME`  
    函数计算应用的运行时栈。配置为 `dotnet`.
- `FUNCTIONS_EXTENSION_VERSION`  
    函数计算应用的平台版本。配置为 `~3`.
- `APPINSIGHTS_INSTRUMENTATIONKEY`  
    用于监控应用程序运行的 [Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview) 写入密钥。  
    若此设置不存在，Application Insights 监控将被禁用。
- `AzureWebJobsDashboard`  
    用于存储应用日志的存储帐户连接字符串。  
    若此设置不存在，存储帐户监控将被禁用。  
    注意存储帐户[不是](https://docs.microsoft.com/en-us/azure/azure-functions/configure-monitoring#disable-built-in-logging)推荐的监控方案。推荐的监控方案是连接 Application Insights. 若使用 Application Insight, 则不应该设置此项。

### 客户端证书验证
后端服务支持验证客户端发送的证书是否有效且与指定的证书一致。注意该功能不是为了验证最终用户的身份，而是为了避免第三方绕过[接口管理服务](https://docs.microsoft.com/en-us/azure/api-management/api-management-key-concepts)而直接调用后端应用程序。如果你不使用接口管理服务，则不应启用这部分设置。
- `ClientAuthentication:Enabled`  
    指定是否启用客户端证书验证。默认值为 `false`.  
    注意启用本设置时，应当同时启用函数应用程序的[客户端证书设置](https://docs.microsoft.com/en-us/azure/app-service/app-service-web-configure-tls-mutual-auth#enable-client-certificates)。
- `ClientAuthentication:DisableChainValidation`  
    指定是否禁用证书链验证。默认值为 `false`.  
    使用自签名证书时，请启用该项设置。
- `ClientAuthentication:CertificateReference`  
    指向要进行匹配的参考证书的 Key Vault 引用，或以 Base64 编码的证书数据。  
    若启用客户端证书验证，则本项必须。
- `WEBSITE_LOAD_USER_PROFILE`  
    是否要加载操作系统的用户配置。  
    在 Windows 环境下，证书数据读取依赖于用户配置。因此，若在 Windows 环境中部署该应用，且启用客户端证书验证，则请将此项设置为 `1`. 否则则不要设置。

### 凭据加密
存储在 Cosmos DB 中的数据是自带加密的。但后端应用程序会对用户凭据进行二次加密，因而即使数据库受到未经授权的访问，用户凭据仍不会失密。
- `Credential:EncryptionKey`  
    用于加密用户凭据的 AES 密钥。使用 Base64 编码。  
    推荐使用 Key Vault 引用。

### 数据访问
后端应用程序使用 [Cosmos DB](https://docs.microsoft.com/en-us/azure/cosmos-db/introduction) 作为数据库方案。本服务只使用 Cosmos DB 中的一个容器，以便降低运营成本。要尽可能降低成本，请使用 Cosmos DB [免费优惠帐户](https://docs.microsoft.com/en-us/azure/cosmos-db/optimize-dev-test#azure-cosmos-db-free-tier)，并使用[共享吞吐量](https://azure.microsoft.com/en-us/blog/sharing-provisioned-throughput-across-multiple-containers-in-azure-cosmosdb/)的容器。
- `Database:ConnectionString`  
    Cosmos DB 数据库帐户的连接字符串。  
    推荐使用 Key Vault 引用。
- `Database:Database`  
    要使用的 Cosmos DB 数据库。注意本项不是数据库帐户名称。  
    应用程序不会尝试创建数据库，请确保数据库已存在。
- `Database:Container`  
    要使用的 Cosmos DB 容器。  
    应用程序不会尝试创建容器，请确保容器已存在。

### 事件收发
应用程序中使用 [Event Grid](https://docs.microsoft.com/en-us/azure/event-grid/overview) 进行事件收发。本服务只使用一个 Event Grid 主题。
- `EventPublish:TopicUri`  
    Event Grid 主题的 URI.
- `EventPublish:TopicKey`  
    Event Grid 主题的密钥。  
    推荐使用 Key Vault 引用。

### 服务基本地址
服务需要知道自己的基本地址，以便动态生成 URI.
- `Host:ServiceBaseAddress`  
    服务最终用户面的基本地址。包括协议名，不要在末尾添加斜线。  
    注意如果使用接口管理服务，应当填写接口管理服务中 API 的基本地址，即面向最终用户的地址。

### 推送通知
向各客户端平台发送推送通知所需的配置。
- `Notification:MaxChannelCountPerPlatform`  
    每个平台的最大推送信道数量。默认值为 `10`.  
    当用户在某个平台有多个设备时，则可能相应地会有多个推送信道。本设置指定最大的信道数量。当用户尝试注册更多信道时，最先注册的信道将被移除。
- `Notification:Retry`  
    向推送服务器发送请求失败后的重试次数。默认值为 `2`.
- `Notification:Windows:PackageSid`  
    Windows UWP 客户端在应用商店中注册的包 SID.  
    如果希望向 Windows UWP 平台发送推送通知，则此项必要。  
    推荐使用 Key Vault 引用。
- `Notification:Windows:Secret`  
    Windows UWP 客户端的 WNS 密钥。  
    如果希望向 Windows UWP 平台发送推送通知，则此项必要。  
    推荐使用 Key Vault 引用。

### 动态数据更新
服务需要定期请求上游服务器，以获取最新数据。本节设置动态数据的轮询计划。
- `ScoreRefreshTimer`  
    成绩轮询计划。使用 [NCRONTAB](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer#ncrontab-expressions) 格式指定。  
    当不在成绩变更高峰时，可以降低轮询频率，以节约成本。  
    轮询不宜过于频繁，尤其是应避免两次轮询重叠。否则将造成并发请求，进而被上游服务器限制访问。  
    一次轮询的所用时间可以通过观察执行指标或通过 Application Insights 获知。

### 周知信息
服务需要当前学期的基础信息。
- `Term:CurrentTerm`  
    当前学期的编号。格式为 `YYYYT`.  
    `YYYY` 为当前学期所处学年的开始自然年。  
    当前学期为第一学期时，`T` 取 `0`, 为第二学期时，`T` 取 `1`.
- `Term:TermStartDate`  
    当前学期的开始日期的 0 时。使用 [ISO 8601](https://en.wikipedia.org/wiki/ISO_8601) 格式。
- `Term:TermEndDate`  
    当前学期随后寒暑假的开始日期的 0 时。使用 ISO 8601 格式。  
    注意这一天实际处于假期。

### 身份令牌
用户使用 [JWT 令牌](https://en.wikipedia.org/wiki/JSON_Web_Token) 进行身份验证。本节设置生成令牌所需的参数。
- `Token:Issuer`  
    服务发放的 JWT 令牌中的 `iss` 与 `aud` 字段。
- `Token:ValidMinutes`  
    服务发放的 JWT 令牌的有效分钟数。默认值为 `60`.
- `Token:SigningKey`  
    用于对令牌进行签名的 SHA-256 密钥。使用 Base64 编码。  
    推荐使用 Key Vault 引用。

### 上游配置
服务需要上游服务器的信息。
- `Upstream:Host`  
    上游服务器的域名或 IP. 不含协议名或任何斜线。  
    建议配置为 `202.202.1.41`.
- `Upstream:UseTls`  
    请求上游服务器时是否使用 TLS. 默认值为 `false`.  
    注意截至 2021 年 2 月 15 日，各上游服务器均不支持 TLS.
- `Upstream:Timeout`  
    上游请求的超时时间，单位为秒。默认值为 `30`.
