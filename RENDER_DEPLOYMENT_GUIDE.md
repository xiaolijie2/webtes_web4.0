# Render 部署指南

本指南将帮助您将 ASP.NET Core 6.0 移动电商API项目部署到 Render 平台。

## 📋 部署前准备

### 1. 项目文件检查
确保您的项目包含以下文件：
- ✅ `Dockerfile` - Docker 构建配置
- ✅ `.dockerignore` - Docker 忽略文件
- ✅ `appsettings.Production.json` - 生产环境配置
- ✅ `render.yaml` - Render 部署配置（可选）

### 2. GitHub 仓库准备
1. 在 GitHub 上创建新仓库或使用现有仓库
2. 将项目代码推送到 GitHub：
```bash
git init
git add .
git commit -m "Initial commit for Render deployment"
git branch -M main
git remote add origin https://github.com/你的用户名/你的仓库名.git
git push -u origin main
```

## 🚀 Render 部署步骤

### 步骤 1: 注册 Render 账号
1. 访问 [Render.com](https://render.com)
2. 点击 "Get Started" 注册账号
3. 建议使用 GitHub 账号登录以便后续连接仓库

### 步骤 2: 创建 Web Service
1. 登录 Render 控制台
2. 点击 "New +" 按钮
3. 选择 "Web Service"
4. 连接您的 GitHub 仓库：
   - 选择 "Connect a repository"
   - 授权 Render 访问您的 GitHub
   - 选择包含项目的仓库

### 步骤 3: 配置 Web Service
填写以下配置信息：

#### 基本设置
- **Name**: `mobile-ecommerce-api`（或您喜欢的名称）
- **Region**: 选择 `Singapore` 或离您用户最近的区域
- **Branch**: `main`
- **Runtime**: `Docker`

#### 构建设置
- **Dockerfile Path**: `./Dockerfile`
- **Docker Context**: `.`

#### 计划选择
- 选择 **Free** 计划（适合测试）
- 或选择付费计划获得更好性能

### 步骤 4: 环境变量配置
在 "Environment Variables" 部分添加以下变量：

```
ASPNETCORE_ENVIRONMENT=Production
JWT_SECRET_KEY=MobileECommerceSecretKey2024ForJWTAuthentication
JWT_ISSUER=MobileECommerceAPI
JWT_AUDIENCE=MobileECommerceClient
JWT_EXPIRE_MINUTES=1440
DATA_PATH=Data
```

**重要提示**：
- `JWT_SECRET_KEY` 应该使用强密码，建议生成新的随机密钥
- 在生产环境中，请使用更安全的密钥管理方式

### 步骤 5: 高级设置（可选）
1. **Health Check Path**: `/`
2. **Auto-Deploy**: 启用（代码推送时自动部署）
3. **Build Filter**: 
   - Include: `**`
   - Ignore: `static_website/**`, `*.md`, `.git/**`

### 步骤 6: 部署
1. 点击 "Create Web Service"
2. Render 将开始构建和部署您的应用
3. 构建过程大约需要 5-10 分钟

## 📊 部署后验证

### 1. 检查部署状态
- 在 Render 控制台查看部署日志
- 确保没有构建错误
- 等待状态变为 "Live"

### 2. 测试 API 端点
部署成功后，您将获得一个 URL，格式如：
`https://your-service-name.onrender.com`

测试以下端点：
```bash
# 健康检查
curl https://your-service-name.onrender.com/

# API 测试
curl https://your-service-name.onrender.com/api/auth/test
```

### 3. 验证功能
- 用户注册和登录
- JWT 令牌生成
- API 端点响应
- 数据持久化

## 🔧 常见问题解决

### 问题 1: 构建失败
**症状**: Docker 构建过程中出错
**解决方案**:
1. 检查 Dockerfile 语法
2. 确保 .csproj 文件正确
3. 查看构建日志中的具体错误信息

### 问题 2: 应用启动失败
**症状**: 构建成功但应用无法启动
**解决方案**:
1. 检查环境变量配置
2. 确保端口配置正确（使用 $PORT）
3. 查看应用日志

### 问题 3: 数据丢失
**症状**: 重新部署后数据消失
**解决方案**:
1. Render 免费计划不提供持久存储
2. 考虑升级到付费计划并配置持久磁盘
3. 或使用外部数据库服务

### 问题 4: 性能问题
**症状**: 应用响应缓慢
**解决方案**:
1. 免费计划有资源限制
2. 考虑升级到付费计划
3. 优化应用代码和数据库查询

## 🔒 安全建议

### 1. 环境变量安全
- 不要在代码中硬编码敏感信息
- 使用强密码作为 JWT 密钥
- 定期轮换密钥

### 2. HTTPS 配置
- Render 自动提供 SSL 证书
- 确保所有 API 调用使用 HTTPS
- 配置 CORS 策略

### 3. 访问控制
- 实施适当的身份验证
- 使用角色基础的访问控制
- 监控异常访问模式

## 📈 监控和维护

### 1. 日志监控
- 定期检查应用日志
- 设置错误告警
- 监控性能指标

### 2. 更新部署
```bash
# 更新代码并推送
git add .
git commit -m "Update: 描述更改内容"
git push origin main
```

### 3. 备份策略
- 定期备份重要数据
- 测试恢复流程
- 考虑使用外部存储服务

## 🌐 自定义域名（可选）

### 1. 添加自定义域名
1. 在 Render 控制台进入您的服务
2. 点击 "Settings" 标签
3. 在 "Custom Domains" 部分添加域名
4. 按照说明配置 DNS 记录

### 2. SSL 证书
- Render 自动为自定义域名提供 SSL 证书
- 证书会自动续期

## 📞 获取帮助

如果遇到问题：
1. 查看 [Render 官方文档](https://render.com/docs)
2. 检查 [Render 社区论坛](https://community.render.com)
3. 联系 Render 技术支持

---

**部署成功后，您的 ASP.NET Core API 将在 Render 平台上稳定运行！** 🎉