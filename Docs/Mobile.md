# Android and iOS feasibility

Yes: the project can support Android and iOS through a .NET MAUI Blazor Hybrid client. Domain and Application are now reusable .NET projects independent of Blazor Server and SQL Server. The current release remains a server-hosted website and does not contain an APK, IPA, native client or authenticated mobile API.

## Recommended implementation

1. Expose authenticated HTTPS API endpoints for the Application use cases with mobile-specific request/response DTOs, authorization and validation. Keep EF Infrastructure and SQL Server credentials on the server.
2. Add a MAUI Blazor Hybrid client and HTTP adapters for the API. Reuse pure report/allocation types; move suitable UI components to a Razor class library, adapting server-specific navigation, cookies and PDF printing.
3. Add supported mobile sign-in/token issuance and secure token storage. The existing cookie authentication is for the web; it is not a mobile bearer-token implementation.
4. Implement mobile PDF download/share using device APIs. Decide explicitly whether offline entry/synchronization is required; Blazor Server needs a live connection and is not an offline mobile engine.
5. Build/test Android on an emulator and physical device. iOS requires the Apple tooling and a Mac build host, signing and provisioning. Store publication is a separate task.

Until a mobile client exists, users can use the hosted responsive web application in their phone browser. Native packaging, authentication, device testing and store distribution remain future work.

Official references:
- https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/tutorials/maui-blazor-web-app?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/configure-multi-targeting
