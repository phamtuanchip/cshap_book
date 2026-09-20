# Chương 12 — Code mẫu: Resource server OIDC/OAuth2 (JWT, scope, PKCE)

Yêu cầu: .NET SDK 10. Chạy trong thư mục này:

```
dotnet run
```

Mở địa chỉ hiển thị trong terminal (thường `http://localhost:5xxx`) bằng trình duyệt hoặc `curl`. Không cấu hình `Oidc:Authority` → chạy chế độ demo với "IdP giả" `/dev/token?sub=an&scope=kho.doc` (chỉ để học). Cấu hình `Oidc:Authority` + `Oidc:Audience` (biến môi trường `Oidc__Authority`) để dùng IdP thật (Keycloak, Entra ID, Auth0...). `/pkce?verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk` cho đúng vector kiểm thử của RFC 7636.
