# 静态网站任务系统

这是一个完全静态的HTML网站版本，专为阿里云虚拟主机等不支持ASP.NET Core的环境设计。

## 功能特性

- ✅ 用户登录/注册系统（基于localStorage）
- ✅ 管理员后台管理
- ✅ 任务管理系统
- ✅ 订单管理
- ✅ 用户资料管理
- ✅ VIP等级系统
- ✅ 充值系统
- ✅ 客服中心
- ✅ 响应式设计

## 文件结构

```
static_website/
├── index.html          # 首页
├── login.html          # 用户登录页
├── admin-login.html    # 管理员登录页
├── home.html           # 用户首页
├── admin.html          # 管理员后台
├── account.html        # 个人中心
├── orders.html         # 订单管理
├── recharge.html       # 充值中心
├── service.html        # 客服中心
├── tasks.html          # 任务中心
├── css/                # 样式文件
├── js/                 # JavaScript文件
│   ├── static-data-manager.js  # 数据管理
│   └── static-auth.js          # 认证管理
├── data/               # 静态数据文件
│   ├── users.json      # 用户数据
│   ├── orders.json     # 订单数据
│   ├── tasks.json      # 任务数据
│   └── ...            # 其他数据文件
└── uploads/            # 上传文件目录
```

## 部署说明

### 1. 阿里云虚拟主机部署

1. 将整个 `static_website` 目录的所有文件上传到虚拟主机的根目录
2. 确保 `index.html` 在根目录下
3. 设置默认首页为 `index.html`
4. 访问您的域名即可使用

### 2. 其他虚拟主机部署

1. 上传所有文件到网站根目录
2. 确保服务器支持静态文件访问
3. 访问网站即可

### 3. 本地测试

使用Python启动本地服务器：
```bash
cd static_website
python -m http.server 8080
```

然后访问 `http://localhost:8080`

## 默认账户

### 用户账户
- 用户名: `user1`
- 密码: `123456`

### 管理员账户
- 用户名: `admin`
- 密码: `admin123`

### 超级管理员账户
- 用户名: `superadmin`
- 密码: `super123`

## 技术特点

### 数据存储
- 使用 `localStorage` 存储用户会话
- 使用静态JSON文件存储业务数据
- 支持数据的读取、更新和持久化

### 认证系统
- 基于localStorage的会话管理
- 支持用户、管理员、超级管理员三种角色
- 自动会话过期和刷新

### 响应式设计
- 支持桌面端和移动端
- 现代化的UI设计
- 流畅的用户体验

## 注意事项

1. **数据持久化**: 由于是静态网站，数据修改只在浏览器本地生效，刷新页面后会恢复到初始状态
2. **跨域问题**: 某些浏览器可能对本地文件有跨域限制，建议通过HTTP服务器访问
3. **文件权限**: 确保上传到服务器的文件有正确的读取权限
4. **HTTPS**: 建议在生产环境中使用HTTPS协议

## 自定义配置

### 修改网站信息
编辑 `data/site_settings.json` 文件来修改网站基本信息

### 添加用户
编辑 `data/users.json` 文件来添加新用户

### 修改任务
编辑 `data/tasks.json` 文件来管理任务

### 更新VIP等级
编辑 `data/vip_levels.json` 文件来配置VIP等级

## 浏览器兼容性

- Chrome 60+
- Firefox 55+
- Safari 12+
- Edge 79+
- 移动端浏览器

## 支持

如有问题，请检查浏览器控制台的错误信息，或联系技术支持。