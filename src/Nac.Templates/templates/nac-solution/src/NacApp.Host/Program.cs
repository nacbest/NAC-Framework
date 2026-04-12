var builder = WebApplication.CreateBuilder(args);

// builder.AddNacFramework(nac =>
// {
//     nac.AddModule<...>();
//     nac.UsePostgreSql();
// });

var app = builder.Build();

// app.UseNacWebApi();
// app.UseNacFramework();

app.MapGet("/", () => "Hello from NacApp!");

app.Run();
