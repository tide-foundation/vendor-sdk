# vendor-sdk
To help integrate Tide flows into your .NET web apps.
## This repo is part of the FIRST PHASE code. All repos that have this message interact and use one another in some way.

## What is it?
Vendor-SDK is a tool to help developers integrate Tide's authentication flows into their .NET web apps. If you've ever wanted, but didn't want to implement:
- User authentication
- User key management
- More to come...
Then this is the tool for you.

What vendor-sdk does is manage the user authentication and (later) key management so you can focus on your application, while Tide takes care of the security. Think of it as similar to OAuth.

This SDK is meant to work in conjunction with the Heimdall JS tool that implements the Tide Button on your website. This Tide button handles the user flow to authenticate with Tide, then redirecting the user back to your website where the vendor-sdk will validate the user's authentication and start a session for them. Once the session is created, the vendor-sdk will redirect the user to a page in which you specified.

## Integration
1. Add the ```TideVendor.SDK``` package to your project (via nuget).

2. Add this line when you're adding services to your web app:
```C#
builder.Services.AddControllersWithViews()
    .AddApplicationPart(typeof(Vendor_SDK.Controllers.TideAuthController).Assembly); 
```

3. Also add the session service to your app, specify your app's session details here:
```C#
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(5);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

4. Also specify what URL you want the vendor-sdk to redirect your client to once authentication has completed:
```C#
builder.Services.Configure<VendorSDKOptions>(options =>
{
    options.RedirectUrl = "https://yoururl.com"; 
});
```

5. After app.UseStaticFiles(), add this:
```C#
 app.UseSession(); 
```

6. Ensure your controller route is controller/action with this line before app.Run():
```C#
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}");
```

### Full example:
```C#
﻿using Vendor.Helpers;
using Vendor.Services;
using Vendor_SDK;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddApplicationPart(typeof(Vendor_SDK.Controllers.TideAuthController).Assembly); // add this

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(5); // add this
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.Configure<VendorSDKOptions>(options =>
{
    options.RedirectUrl = "http://localhost:5231/hello"; // add this
});


var services = builder.Services;
services.AddDbContext<DataContext>();
services.AddScoped<IUserService, UserService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors(builder => {
    builder.AllowAnyOrigin();
    builder.AllowAnyMethod();
    builder.AllowAnyHeader();
});

app.UseSession(); // add this

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

### **Sample project that uses vendor-sdk can be found [here](https://github.com/tide-foundation/sample-vendor/tree/main)**
