# UCQU 后端 API
本文档介绍 UCQU 后端各公开接口的定义。

## 基准 URI
本服务后端的基准 URI 为 `https://api.ucqu.dl444.net/v1`. 如无特别说明，后文所表示的所有接口 URI 均相对于此。

如果你自行部署该服务，请根据自己的实际情况进行替换。

## 返回模型
本服务中所有接口若返回对象，则返回对象均遵循如下模型：
```json
{
    "success": "bool",
    "resource": {},
    "message": "string"
}
```
- `success`  
    请求的操作是否成功完成
- `resource`  
    操作的具体返回对象 (如有)
- `message`  
    任何错误或警告信息 (如有)

在部分接口中，成功的 HTTP 状态不意味着逻辑的成功。此时，请检查 `success` 与 `message` 字段。我们将对相关接口进行详细说明。

`resource` 字段将包含各接口所返回的具体数据。这些数据的模型请参见具体的接口说明。

本文档模型中所使用的 `dateTime` 类型，指将时间以 ISO 8601 标准序列化后的字符串。例如，东八区 2021 年 1 月 1 日早九时表示为 `2021-01-01T09:00:00+08:00`.

## 身份认证
本服务中凡需要身份认证的接口均使用 JWT 令牌进行验证。当你成功调用登录接口登录时，你将收到一个令牌。调用需要身份认证的接口时，请通过 `Authorization` 请求头发送这一令牌。
```
Authorization: Bearer {token}
```
其中 `{token}` 为获得的令牌。

若调用需要身份认证的接口时使用的令牌无效或已过期，你将收到状态为 401 的响应。此时，请重新调用登录接口，获取新的令牌。

## 接口定义
以下介绍各接口的详细定义。

## 登录
### 请求
**`POST /signIn/{createAccount}`**  
无需身份认证

路径参数
- `{createAccount}`  
    是否创建新用户  
    提供值为 `true` 时，服务将在用户不存在时创建用户  
    提供值为 `false` 时，服务将不尝试创建新帐户

内容
```json
{
    "studentId": "string",
    "passwordHash": "string"
}
```
- `studentId`  
    用户的学号
- `passwordHash`  
    用户的密码哈希  
    关于此哈希的计算方法，请参见后文。

### 响应
`200`  
**登录成功**

你已成功登录，并获得访问令牌。

返回对象
```json
{
    "completed": "bool",
    "token": "string"
}
```
- `completed`  
    恒为 `true`
- `token`  
    访问令牌  
    调用需要身份认证的接口时，使用此令牌。

`200`  
**登录成功 - 联机验证失败**

由于教务系统故障，我们无法联机验证你提供的凭据。但你提供的凭据与最近一次成功登录时使用的凭据一致。  

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
同 **登录成功** 情况。

`202`  
**登录成功 - 正在进行准备**

登录已经成功，但我们需要额外时间进行准备。请轮询我们在响应中提供的接口以获取准备进度。

这种情况出现在用户首次登录时。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象
```json
{
    "completed": "bool",
    "location": "string",
    "token": "string",
}
```
- `completed`  
    恒为 `false`
- `location`  
    轮询接口  
    轮询此接口，获取准备进度。请参见下文关于用户准备进度接口的详细说明。
- `token`  
    访问令牌  
    调用需要身份认证的接口时，使用此令牌。

`401`  
**登录失败 - 凭据无效**

你提供的凭据不正确。或者，我们无法联机验证你提供的凭据，且你提供的凭据与最近一次成功登录时使用的凭据不一致。再或，我们无法联机验证你提供的凭据，且你之前从未成功登录。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
无返回对象。

`401`  
**登录失败 - 联机验证失败**

我们无法联机验证你提供的凭据，且你之前从未成功登录。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
无返回对象。

`401`  
**登录失败 - 用户不存在**

指定的用户不存在，而请求中未指定 `createAccount` 参数。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
无返回对象。

### 接入说明
密码哈希的计算方法如下：

1. 对密码文本计算 MD5. 使用 ASCII 编码。
2. 取结果前 15 个字节的十六进制文本表示。不包含前缀 `x`, 字母使用大写。
3. 在文本后拼接 `10611`, 在文本前拼接用户名。
4. 对拼接后的文本再次计算 MD5. 同样使用 ASCII 编码。
5. 取结果前 15 个字节的十六进制文本表示。不包含前缀 `x`, 字母使用大写。
6. 得到的文本即为所求。

以下是一种 C# 实现。
```cs
public static string GetPasswordHash(string username, string password)
{
    MD5 md5 = MD5.Create();
    byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
    byte[] hash = md5.ComputeHash(passwordBytes);
    StringBuilder builder = new StringBuilder(username.Length + 35);
    builder.Append(username);
    for (int i = 0; i < 15; i++)
    {
        builder.Append(hash[i].ToString("X2"));
    }
    builder.Append("10611");
    string hashStr = builder.ToString();
    passwordBytes = Encoding.ASCII.GetBytes(hashStr);
    hash = md5.ComputeHash(passwordBytes);
    builder = new StringBuilder(30);
    for (int i = 0; i < 15; i++)
    {
        builder.Append(hash[i].ToString("X2"));
    }
    return builder.ToString();
}
```
在用户显式登录时，客户端应当附加 `createAccount` 路径参数，以便在用户不存在时创建。

客户端应当将用户输入的凭据安全地保存下来，供更新令牌时使用。在隐式更新令牌时，客户端不得附加 `createAccount` 参数。客户端应当同时提供退出登录并清除本地已保存凭据的命令。

当客户端在调用需要身份认证的接口时收到状态为 401 的响应时，应当首先尝试调用登录接口更新令牌。若登录时再次收到状态为 401 的响应时，说明用户已经在教务系统更改凭据，或已从服务中删除。此时，客户端应当退出登录并清除本地已保存的凭据。

## 用户准备进度
### 请求
**`GET /userInit/{id}`**  
无需身份认证

路径参数
- `{id}`  
    用户准备任务 ID.  
    你无需自行填写该参数。登录接口返回的 URI 中已经包含了这一参数。

### 响应
`200`  
**准备完成**

我们已经完成准备，你可以继续执行。

返回对象
```json
{
    "taskId": "string",
    "completed": "bool",
    "lastUpdateTimestamp": "long"
}
```
- `taskId`  
    用户准备任务 ID
- `completed`  
    恒为 `true`
- `lastUpdateTimestamp`  
    最后一次进度更新时间  
    使用 Unix 时间标记法，精确到秒。

`202`  
**准备进行中**

准备仍在进行，建议继续等待。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象
```json
{
    "taskId": "string",
    "completed": "bool",
    "lastUpdateTimestamp": "long"
}
```
- `taskId`  
    用户准备任务 ID
- `completed`  
    恒为 `false`
- `lastUpdateTimestamp`  
    最后一次进度更新时间  
    使用 Unix 时间标记法，精确到秒。

### 接入说明
客户端在登录时若收到状态为 202 的响应，则应当向响应指定的本接口轮询。推荐使用 2 至 5 秒为轮询间隔。

多数情况下，在本接口返回状态为 200 的响应之前，客户端不应继续执行其它请求。但若等待 60 秒后，本接口的返回状态仍为 202, 则应放弃等待，继续执行。

## 获取学生信息
### 请求
**`GET /studentInfo`**  
需要身份验证

### 响应
`200`  
**获取成功**

成功获取到了相关数据。

返回对象
```json
{
    "studentId": "string",
    "name": "string",
    "year": "int",
    "major": "string",
    "class": "string",
    "secondMajor": "string",
    "calendarSubscriptionId": "string"
}
```
- `studentId`  
    用户的学号
- `name`  
    用户的姓名
- `year`  
    用户的入学年份
- `major`  
    用户修读的专业
- `class`  
    用户所属的行政班级
- `secondMajor`  
    用户修读的第二专业 (如有)
- `calendarSubscriptionId`  
    用户的课程表日历订阅 ID (如有)

`200`  
**获取成功 - 缓存数据**

成功获取到了服务缓存的数据，但由于教务系统不可用，该数据可能不能反映最新状态。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
同 **获取成功** 情况。

`502`  
**获取失败 - 上游不可用**

服务没有缓存数据，且教务系统不可用。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
无返回对象。

`503`  
**获取失败 - 服务不可用**

服务当前不可用。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
无返回对象。

### 接入说明
客户端应当将获取到的信息缓存在本地。在应用程序启动时，应当首先显示缓存数据，并在后台通过网络更新。

当客户端在调用本接口时收到状态为 401 的响应时，应当首先尝试调用登录接口更新令牌。若登录时再次收到状态为 401 的响应时，说明用户已经在教务系统更改凭据。此时，客户端应当退出登录并清除本地已保存的凭据。

当客户端收到状态为 502 或 503 的响应时，应当在等待一段时间后重试，推荐等待时间为 5 至 10 秒。若再次收到此类响应，应当放弃获取，并展示先前缓存的信息。

## 获取课程表
### 请求
**`GET /schedule`**  
需要身份验证

### 响应
`200`  
**获取成功**

成功获取到了相关数据。

返回对象
```json
{
    "studentId": "string",
    "weeks": [
        {
            "weekNumber": "int",
            "entries": [
                {
                    "name": "string",
                    "lecturer": "string",
                    "room": "string",
                    "dayOfWeek": "int",
                    "startSession": "int",
                    "endSession": "int"
                }
            ]
        }
    ]
}
```
- `studentId`  
    用户的学号
- `weeks`  
    课表中的所有周
- `weekNumber`  
    该周的周数  
    第一周为 1.
- `entries`  
    该周的所有课程
- `name`  
    该课程的名称
- `lecturer`  
    该课程的任课教师
- `room`  
    该课程的上课教室
- `dayOfWeek`  
    该课程在周几上课  
    周一为 1, 周日为 7.
- `startSession`  
    该课程的起始节数  
    第一节课开始的课程，本字段为 1.
- `endSession`  
    该课程的终止节数  
    第一节课结束的课程，本字段为 1.

`200`  
**获取成功 - 缓存数据**

成功获取到了服务缓存的数据，但由于教务系统不可用，该数据可能不能反映最新状态。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
同 **获取成功** 情况。

`502`  
**获取失败 - 上游不可用**

服务没有缓存数据，且教务系统不可用。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
无返回对象。

`503`  
**获取失败 - 服务不可用**

服务当前不可用。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
无返回对象。

### 接入说明
客户端应当将获取到的信息缓存在本地。在应用程序启动时，应当首先显示缓存数据，并在后台通过网络更新。

当客户端在调用本接口时收到状态为 401 的响应时，应当首先尝试调用登录接口更新令牌。若登录时再次收到状态为 401 的响应时，说明用户已经在教务系统更改凭据。此时，客户端应当退出登录并清除本地已保存的凭据。

当客户端收到状态为 502 或 503 的响应时，应当在等待一段时间后重试，推荐等待时间为 5 至 10 秒。若再次收到此类响应，应当放弃获取，并展示先前缓存的信息。

## 获取考试计划
### 请求
**`GET /exams`**  
需要身份验证

### 响应
`200`  
**获取成功**

成功获取到了相关数据。

返回对象
```json
{
    "studentId": "string",
    "exams": [
        {
            "name": "string",
            "shortName": "string",
            "credit": "double",
            "category": "string",
            "type": "int",
            "startTime": "dateTime",
            "endTime": "dateTime",
            "week": "int",
            "dayOfWeek": "int",
            "location": "string",
            "shortLocation": "string",
            "seating": "int"
        }
    ]
}
```
- `studentId`  
    用户的学号
- `exams`  
    用户的所有考试计划
- `name`  
    考试科目的全称  
    包含课程号与课程名称。
- `shortName`  
    考试科目的名称  
    仅包含课程名称，不含课程号。
- `category`  
    考试科目的类型 (如有)
- `type`  
    考试的组织形式 (如有)
- `startTime`  
    考试的开始日期与时间
- `endTime`  
    考试的结束日期与时间
- `week`  
    考试所在的周数  
    第一周进行的考试，本字段为 1.
- `location`  
    考试组织的完整地点  
    包含校区，楼栋，与教室。
- `shortLocation`  
    考试组织的地点  
    仅包含教室。教室名称已包含校区与楼栋信息。
- `seating`  
    用户的座号  

`200`  
**获取成功 - 缓存数据**

成功获取到了服务缓存的数据，但由于教务系统不可用，该数据可能不能反映最新状态。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
同 **获取成功** 情况。

`502`  
**获取失败 - 上游不可用**

服务没有缓存数据，且教务系统不可用。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
无返回对象。

`503`  
**获取失败 - 服务不可用**

服务当前不可用。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
无返回对象。

### 接入说明
本接口返回的数据仅限在教务系统中可以获取的信息，不一定包含用户的所有考试信息。建议客户端对此作出提示。

客户端应当将获取到的信息缓存在本地。在应用程序启动时，应当首先显示缓存数据，并在后台通过网络更新。

当客户端在调用本接口时收到状态为 401 的响应时，应当首先尝试调用登录接口更新令牌。若登录时再次收到状态为 401 的响应时，说明用户已经在教务系统更改凭据。此时，客户端应当退出登录并清除本地已保存的凭据。

当客户端收到状态为 502 或 503 的响应时，应当在等待一段时间后重试，推荐等待时间为 5 至 10 秒。若再次收到此类响应，应当放弃获取，并展示先前缓存的信息。

## 获取成绩表
### 请求
**`GET /score/{secondMajor}`**  
需要身份验证

路径参数
- `{secondMajor}`  
    主修专业 / 第二专业标记  
    填写值为 0 或 1 时，返回主修专业成绩。  
    填写值为 2 时，返回第二专业成绩。

### 响应
`200`  
**获取成功**

成功获取到了相关数据。

返回对象
```json
{
    "studentId": "string",
    "isSecondMajor": "bool",
    "gradePoint": "double",
    "terms": [
        {
            "beginningYear": "int",
            "endingYear": "int",
            "termNumber": "int",
            "gradePoint": "double",
            "courses": [
                {
                    "name": "string",
                    "shortName": "string",
                    "credit": "double",
                    "category": "string",
                    "isInitialTake": "bool",
                    "isMakeup": "bool",
                    "score": "int",
                    "isSecondMajor": "bool",
                    "comment": "string",
                    "lecturer": "string",
                    "shortLecturer": "string",
                    "obtainedTime": "dateTime",
                    "gradePoint": "double"
                }
            ]
        }
    ]
}
```
- `studentId`  
    用户的学号
- `isSecondMajor`  
    该成绩表是否为第二专业成绩
- `gradePoint`  
    该成绩表中所有科目的四分制平均绩点
- `terms`  
    成绩表中的所有学期
- `beginningYear`  
    该学期所属学年的起始年度
- `endingYear`  
    该学期所属学年的结束年度
- `termNumber`  
    该学期在学年中的学期序号
- `gradePoint`  
    该学期的四分制平均绩点
- `courses`  
    该学期已公布成绩科目
- `name`  
    考试科目的全称  
    包含课程号与课程名称。
- `shortName`  
    考试科目的名称  
    仅包含课程名称，不含课程号。
- `category`  
    考试科目的类型 (如有)
- `isInitialTake`  
    该成绩是否为初修成绩
- `isMakeup`  
    该成绩是否为补考成绩
- `score`  
    该科目的考试成绩  
    采用两级制或五级制成绩的科目，将按教务规定换算为数值分数。
- `comment`  
    成绩备注 (如有)  
    可能的值包括 `补考`, `缺考`, `补考(缺考)` 等
- `lecturer`  
    该课程的授课教师全称 (如有)  
    包含教师姓名与编号。
- `shortLecturer`  
    该课程的授课教师姓名 (如有)  
    仅包含教师姓名，不含编号。
- `obtainedTime`  
    成绩的获得时间
- `gradePoint`  
    该课程的四分制绩点

`200`  
**获取成功 - 缓存数据**

成功获取到了服务缓存的数据，但由于教务系统不可用，该数据可能不能反映最新状态。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
同 **获取成功** 情况。

`502`  
**获取失败 - 上游不可用**

服务没有缓存数据，且教务系统不可用。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
无返回对象。

`503`  
**获取失败 - 服务不可用**

服务当前不可用。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
无返回对象。

### 接入说明
本接口返回的数据为在新教务系统中获取的信息，而推荐免试攻读研究生等考核使用老教务网中的信息，二者可能存在差异。建议客户端对此作出提示。

客户端应当将获取到的信息缓存在本地。在应用程序启动时，应当首先显示缓存数据，并在后台通过网络更新。

当客户端在调用本接口时收到状态为 401 的响应时，应当首先尝试调用登录接口更新令牌。若登录时再次收到状态为 401 的响应时，说明用户已经在教务系统更改凭据。此时，客户端应当退出登录并清除本地已保存的凭据。

当客户端收到状态为 502 或 503 的响应时，应当在等待一段时间后重试，推荐等待时间为 5 至 10 秒。若再次收到此类响应，应当放弃获取，并展示先前缓存的信息。

## 获取课程表日历订阅
### 请求
**`GET /calendar/{studentId}/{subscriptionId}`**  
无需身份认证

路径参数
- `{studentId}`  
    用户的学号
- `{subscriptionId}`  
    用户的课程表日历订阅 ID  
    该值可在学生信息接口中获得。

### 响应
`200`  
**获取成功**

成功获取到课程表日历订阅信息。

返回对象  
课程表日历订阅信息。该返回对象无外层的返回模型。

`200`  
**获取失败 - 订阅地址无效**

指定学号的用户不存在，或其订阅 ID 与提供不符。

返回对象  
空白的日历订阅信息。该返回对象无外层的返回模型。

`503`  
**获取失败 - 服务不可用**

服务当前不可用。

返回对象  
无返回对象。

### 接入说明
客户端通常不应当直接调用该接口。客户端应当将该接口的 URI 呈现给用户，令用户将其以订阅的形式添加到自己的日历，由日历应用程序调用该接口。

要获取订阅 ID 信息，调用 获取学生信息 接口，而非本接口。

若用户不希望使用日历订阅，客户端可以调用该接口，并将返回的文本写入至一个 ICS 文件中，令用户将这一文件导入日历。但通过这种方式导入订阅，将无法获得自动更新。

本接口无需身份认证，任何知晓订阅地址的人均可访问该用户的课程表，建议客户端对此作出提示。你可以调用下文描述的地址重置接口，更换订阅地址，原有的任何订阅地址将失效。

当订阅地址无效时，本接口将返回空日历信息，而非任何错误状态。如此，在订阅地址重置时，通过原有订阅获得的日历信息将在更新中被移除。

## 重置课程表日历订阅地址
### 请求
**`POST /calendar`**  
需要身份认证

### 响应
`200`  
**重置成功**

成功重置课程表日历订阅地址。

返回对象  
```json
{
    "subscriptionId": "string"
}
```
- `subscriptionId`  
    课程表日历订阅 ID

`503`  
**重置失败 - 服务不可用**

服务当前不可用。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
无返回对象。

### 接入说明
本接口仅用于更换日历订阅 ID. 客户端不应使用此接口来获取日历订阅 ID. 要获取日历订阅 ID, 调用 获取学生信息 接口。

当客户端收到状态为 502 或 503 的响应时，应当在等待一段时间后重试，推荐等待时间为 5 至 10 秒。若再次收到此类响应，应当放弃操作，并展示错误信息。

## 删除用户
### 请求
**`DELETE /user`**  
需要身份认证

### 响应
`200`  
**删除成功**

成功删除用户数据。

返回对象  
无返回对象。

`503`  
**删除失败 - 服务不可用**

服务当前不可用。

此时，在返回模型的 `message` 字段中，将包含客户端可用的说明文本。

返回对象  
无返回对象。

### 接入说明
调用本接口将从服务中删除与该用户相关的所有数据，包括但不限于存储的凭据，学生信息，课程表，考试安排表，成绩单。原有的日历订阅也将失效。

客户端应当明确区分在本地退出登录与从服务中删除帐户的区别，并提醒用户注意。

当客户端收到状态为 502 或 503 的响应时，应当在等待一段时间后重试，推荐等待时间为 5 至 10 秒。若再次收到此类响应，应当放弃操作，并展示错误信息。

删除帐户成功后，客户端应当立即退出登录，清除任何本地存储的凭据，并清空缓存的数据。

若用户多端登录，则其它客户端的后续请求将返回状态为 401 的响应。按照本文档推荐的方式实现的客户端应当退出登录并清除所有本地数据。

## 获取公开基础信息
### 请求
**`GET /wellknown`**  
无需身份认证

### 响应
`200`  
**获取成功**

成功获取到数据。

返回对象
```json
{
    "currentTerm": "string",
    "termStartDate": "dateTime",
    "termEndDate": "dateTime"
}
```
- `currentTerm`  
    当前的学期编号
- `termStartDate`  
    本学期的开始日期  
    本学期包含该日。
- `termEndDate`  
    本学期的结束日期  
    本学期不含该日。

### 接入说明
客户端应当缓存学期信息。当前日期位于学期开始日与结束日之间时，客户端可以直接使用缓存数据。

学期结束后，客户端应当定期调用这一接口，以获取最新的信息。

## 获取开发人员消息
### 请求
**`GET /devMessage/{platform}`**  
无需身份验证

路径参数
- `{platform}`  
    当前平台    
    当提供值为 `android` 时，表示当前平台为 Android.  
    当提供值为 `appleDesktop`, `macOs`, `osX` 时，表示当前平台为 macOS.  
    当提供值为 `appleMobile`, `iOs`, `iPadOs` 时，表示当前平台为 iOS.  
    当提供值为 `web` 时，表示当前平台为 Web 前端。  
    当提供值为 `windows` 时，表示当前平台为 Windows.

### 响应
`200`  
**获取成功**

成功获取到了开发人员消息。

返回对象
```json
{
    "messages": [
        {
            "id": "string",
            "title": "string",
            "content": "string",
            "targetPlatforms": "int",
            "time": "dateTime",
            "archived": "bool"
        }
    ]
}
```
- `messages`  
    所有获取到的消息
- `id`  
    消息的唯一 ID
- `title`  
    消息的标题
- `content`  
    消息的内容
- `targetPlatforms`  
    消息的目标平台  
    该字段为位标识，详见后文中的说明。
- `time`  
    消息的发布时间
- `archived`  
    恒为 `false`

### 接入说明
鉴于上游教务系统不稳定不可控，有必要在客户端中添加开发人员与用户的沟通渠道。

建议客户端维护一个本地记录，当获取到新消息时进行通知。但切忌打扰用户。

返回对象中的 `targetPlatforms` 字段为位标识，每个二进制位表示一个平台。其枚举值如下：
- `0b00001`  
    Android
- `0b00010`  
    macOS
- `0b00100`  
    iOS
- `0b01000`  
    Web
- `0b10000`  
    Windows

可以将多个枚举值通过位或运算组合起来，表示消息同时适用于多个平台。
