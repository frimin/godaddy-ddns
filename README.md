# godaddy-ddns

自动更新当前 IP 地址到 godaddy 域名解析中。

# 构建

## 本地构建

1.安装 [dotnet6.0](https://dot.net/)

2.执行 make

3.运行 `dotnet build/godaddy-ddns.dll <参数...>`

## 构建 Docker 镜像

安装 docker 和 docker-compose，执行:

    docker-compose build && docker-compose up

# 参数

API-KEY 从 https://developer.godaddy.com/keys/ 获取。

| 命令行参数 | 环境变量参数 | 描述 |
|  ----  |  ----  |  ----  |
| --key  | GODADDY_DDNS_KEY | API-KEY |
| --secret  | GODADDY_DDNS_SECRET | API-SECRET |
| --name  | GODADDY_DDNS_NAME | 解析名称 (例如 name.abc.com 中的 name) |
| --domain | GODADDY_DDNS_DOMAIN | 域名名称 (例如 name.abc.com 中的 abc.com) |
| --get-ip-url | GODADDY_DDNS_GET_IP_URL | 重新指定获取当前 IP 的服务地址，默认为 https://icanhazip.com |
| --interval | GODADDY_DDNS_INTERVAL | 检查间隔，单位秒，默认 300 |
| --ttl | GODADDY_DDNS_TLL | 指定域名TTL，默认 600 |
| --ipv6 | GODADDY_DDNS_IPV6 | 强制使用 IPV6 请求当前公网地址，默认 0 |
| --full-log | GODADDY_DDNS_FULL_LOG | 输出完整日志，默认 0 |
| --without-loop-check | GODADDY_DDNS_WITHOUT_LOOP_CHECK | 不要循环检查，默认 0|

# 使用 IPV6 

使用前确保机器已经分配了IPV6地址，使用 Docker 镜像时建议直接用 host 网络模式。

# LICENSE

MIT