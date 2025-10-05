# 使用官方的 .NET 6.0 SDK 镜像作为构建环境
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# 复制 csproj 文件并还原依赖项
COPY *.csproj ./
RUN dotnet restore

# 复制所有源代码并构建应用程序
COPY . ./
RUN dotnet publish -c Release -o out

# 使用官方的 .NET 6.0 运行时镜像作为运行环境
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app

# 复制构建输出
COPY --from=build-env /app/out .

# 创建数据目录并设置权限
RUN mkdir -p /app/Data && chmod 755 /app/Data

# 设置环境变量
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:$PORT

# 暴露端口（Render 会动态分配端口）
EXPOSE $PORT

# 启动应用程序
ENTRYPOINT ["dotnet", "MobileECommerceAPI.dll"]