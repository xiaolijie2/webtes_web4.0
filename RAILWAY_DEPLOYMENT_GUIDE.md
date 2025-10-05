# Railway 部署指南 - ASP.NET Core 6.0 移动电商API

## 概述

本指南将帮助您将 ASP.NET Core 6.0 移动电商API项目部署到 Railway 平台。Railway 是一个现代化的云平台，提供免费的部署服务，无需信用卡，支持自动部署和免费域名。

## Railway 平台优势

- ✅ **完全免费** - 无需信用卡，提供慷慨的免费额度
- ✅ **Docker 支持** - 原生支持 Docker 容器部署
- ✅ **自动部署** - 从 GitHub 自动部署，支持 CI/CD
- ✅ **免费域名** - 提供 `.railway.app` 免费子域名
- ✅ **免费 SSL** - 自动配置 HTTPS 证书
- ✅ **环境变量管理** - 安全的环境变量配置
- ✅ **实时日志** - 实时查看应用程序日志
- ✅ **数据持久化** - 支持数据卷挂载

## 前置要求

1. **GitHub 账户** - 用于代码托管
2. **Railway 账户** - 注册地址：https://railway.app
3. **项目代码** - 已推送到 GitHub 仓库

## 部署步骤

### 第一步：准备 GitHub 仓库

确保您的项目代码已经推送到 GitHub 仓库：
```
https://github.com/xiaolijie2/webtes_web4.0.git
```

### 第二步：注册 Railway 账户

1. 访问 https://railway.app
2. 点击 "Start a New Project"
3. 使用 GitHub 账户登录（推荐）
4. 授权 Railway 访问您的 GitHub 仓库

### 第三步：创建新项目

1. 在 Railway 控制台中，点击 "New Project"
2. 选择 "Deploy from GitHub repo"
3. 选择您的仓库：`xiaolijie2/webtes_web4.0`
4. Railway 会自动检测到 Dockerfile 并开始构建

### 第四步：配置环境变量

在 Railway 项目设置中添加以下环境变量：

#### 必需的环境变量：

```bash
# ASP.NET Core 环境
ASPNETCORE_ENVIRONMENT=Production

# JWT 配置
JWT_SECRET_KEY=your-super-secret-jwt-key-at-least-32-characters-long
JWT_ISSUER=MobileECommerceAPI
JWT_AUDIENCE=MobileECommerceAPI
JWT_EXPIRE_MINUTES=60

# 数据存储路径
DATA_PATH=/app/Data

# 端口配置（Railway 自动设置）
PORT=5000
```

#### 环境变量设置步骤：

1. 在 Railway 项目页面，点击您的服务
2. 进入 "Variables" 标签页
3. 点击 "New Variable" 添加每个环境变量
4. 输入变量名和值
5. 点击 "Add" 保存

### 第五步：部署配置

Railway 会自动使用项目中的配置文件：

- **railway.json** - Railway 平台配置
- **Dockerfile** - Docker 构建配置
- **.dockerignore** - Docker 构建忽略文件

### 第六步：监控部署

1. 在 Railway 控制台中查看部署日志
2. 等待构建完成（通常需要 2-5 分钟）
3. 部署成功后，Railway 会提供一个公共 URL

### 第七步：访问应用

部署成功后，您将获得一个类似以下格式的 URL：
```
https://your-app-name.railway.app
```

## 验证部署

### 1. 健康检查

访问以下端点验证 API 是否正常运行：
```
GET https://your-app-name.railway.app/api/health
```

### 2. API 文档

访问 Swagger 文档：
```
https://your-app-name.railway.app/swagger
```

### 3. 测试端点

测试一些基本的 API 端点：
```bash
# 获取国家代码
GET https://your-app-name.railway.app/api/countrycode

# 用户注册测试
POST https://your-app-name.railway.app/api/auth/register
```

## 高级配置

### 自定义域名

1. 在 Railway 项目设置中，进入 "Settings" 标签页
2. 在 "Domains" 部分添加自定义域名
3. 配置 DNS 记录指向 Railway 提供的 CNAME

### 数据持久化

Railway 自动处理数据持久化，您的 `/app/Data` 目录中的 JSON 文件将被保留。

### 环境变量管理

- 使用 Railway 控制台管理环境变量
- 支持从 `.env` 文件导入
- 支持变量模板和引用

## 故障排除

### 常见问题

1. **构建失败**
   - 检查 Dockerfile 语法
   - 确保所有依赖项都在 .csproj 文件中

2. **应用启动失败**
   - 检查环境变量配置
   - 查看 Railway 部署日志

3. **端口问题**
   - Railway 自动设置 PORT 环境变量
   - 确保应用监听 `$PORT` 端口

4. **数据丢失**
   - 检查数据目录权限
   - 确保使用正确的数据路径

### 查看日志

在 Railway 控制台中：
1. 选择您的项目
2. 点击服务名称
3. 查看 "Deployments" 和 "Logs" 标签页

## 性能优化

### 1. 构建优化

- 使用多阶段 Docker 构建
- 优化 .dockerignore 文件
- 缓存 NuGet 包

### 2. 运行时优化

- 配置适当的日志级别
- 使用生产环境配置
- 启用响应压缩

### 3. 监控

- 使用 Railway 内置监控
- 配置健康检查端点
- 监控资源使用情况

## 安全最佳实践

1. **环境变量安全**
   - 使用强密码和密钥
   - 定期轮换敏感信息
   - 不要在代码中硬编码密钥

2. **HTTPS**
   - Railway 自动提供 SSL 证书
   - 强制使用 HTTPS

3. **API 安全**
   - 实施适当的身份验证
   - 使用 CORS 策略
   - 验证输入数据

## 成本管理

Railway 免费计划包括：
- 500 小时/月的运行时间
- 1GB RAM
- 1GB 存储空间
- 100GB 网络传输

对于生产环境，考虑升级到付费计划以获得更多资源。

## 支持和资源

- **Railway 文档**: https://docs.railway.app
- **Railway 社区**: https://discord.gg/railway
- **GitHub 仓库**: https://github.com/xiaolijie2/webtes_web4.0

## 总结

Railway 为 ASP.NET Core 应用提供了一个优秀的部署平台，具有以下优势：

1. **简单易用** - 几分钟内完成部署
2. **成本效益** - 免费计划适合开发和小型项目
3. **自动化** - 支持 CI/CD 和自动部署
4. **可扩展** - 根据需要轻松扩展资源

按照本指南，您应该能够成功将 ASP.NET Core 6.0 移动电商API部署到 Railway 平台。