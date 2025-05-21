using Neo4j.Driver;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Neo4j para acesso ao banco
builder.Services.AddSingleton<IDriver>(GraphDatabase.Driver(
    "bolt://localhost:7687",
    AuthTokens.Basic("neo4j", "12345678")
));

// Configuração do CORS para permitir requisições do frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Caso precise usar cookies ou autenticação
    });
});

// Configuração dos controllers
builder.Services.AddControllers();

// Configuração do Swagger para documentação da API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ⚠️ UseCors deve vir antes de qualquer outra coisa que processe as requisições
app.UseCors("AllowLocalhost");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirecionamento para HTTPS
app.UseHttpsRedirection();

// (Se você tiver autenticação/autorizações, use aqui: app.UseAuthorization())

// Mapeamento dos controllers
app.MapControllers();

app.Run();
