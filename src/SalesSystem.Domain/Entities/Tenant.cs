using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SalesSystem.Domain.Entities;

public class Tenant : BaseEntity
{
    [BsonElement("name")]      public string         Name      { get; set; } = string.Empty;
    [BsonElement("subdomain")] public string         Subdomain { get; set; } = string.Empty;
    [BsonElement("document")]  public string?        Document  { get; set; }
    [BsonElement("email")]     public string?        Email     { get; set; }
    [BsonElement("phone")]     public string?        Phone     { get; set; }
    [BsonElement("isActive")]  public bool           IsActive  { get; set; } = true;
    [BsonElement("plan")]      public string         Plan      { get; set; } = "standard";
    [BsonElement("uiConfig")]  public TenantUiConfig UiConfig  { get; set; } = new();
    [BsonElement("modules")]   public List<string>   Modules   { get; set; } = ["produtos","clientes","estoque","pedidos","financeiro"];
    [BsonElement("settings")]  public TenantSettings Settings  { get; set; } = new();
    [BsonElement("vocabulary")] public TenantVocabulary Vocabulary { get; set; } = new();
}

public class TenantVocabulary
{
    [BsonElement("presetId")] public string                             PresetId  { get; set; } = "confeitaria";
    [BsonElement("overrides")] public Dictionary<string, VocabularyTermDoc> Overrides { get; set; } = [];
}

public class VocabularyTermDoc
{
    [BsonElement("singular")]           public string Singular           { get; set; } = string.Empty;
    [BsonElement("plural")]             public string Plural             { get; set; } = string.Empty;
    [BsonElement("articleSingular")]    public string ArticleSingular    { get; set; } = "a";
    [BsonElement("articlePlural")]      public string ArticlePlural      { get; set; } = "as";
    [BsonElement("indefiniteSingular")] public string IndefiniteSingular { get; set; } = "uma";
}

public class TenantUiConfig
{
    [BsonElement("logoUrl")]            public string? LogoUrl            { get; set; }
    [BsonElement("faviconUrl")]         public string? FaviconUrl         { get; set; }
    [BsonElement("companyDisplayName")] public string? CompanyDisplayName { get; set; }
    [BsonElement("footerText")]         public string? FooterText         { get; set; }
    [BsonElement("primaryColor")]       public string  PrimaryColor       { get; set; } = "#2563EB";
    [BsonElement("primaryDark")]        public string  PrimaryDark        { get; set; } = "#1D4ED8";
    [BsonElement("primaryLight")]       public string  PrimaryLight       { get; set; } = "#DBEAFE";
    [BsonElement("accentColor")]        public string  AccentColor        { get; set; } = "#F59E0B";
    [BsonElement("dangerColor")]        public string  DangerColor        { get; set; } = "#EF4444";
    [BsonElement("successColor")]       public string  SuccessColor       { get; set; } = "#22C55E";
    [BsonElement("sidebarBg")]          public string  SidebarBg          { get; set; } = "#1E293B";
    [BsonElement("sidebarText")]        public string  SidebarText        { get; set; } = "#F1F5F9";
    [BsonElement("sidebarAccent")]      public string  SidebarAccent      { get; set; } = "#2563EB";
    [BsonElement("topbarBg")]           public string  TopbarBg           { get; set; } = "#FFFFFF";
    [BsonElement("bodyBg")]             public string  BodyBg             { get; set; } = "#F8FAFC";
    [BsonElement("fontFamily")]         public string  FontFamily         { get; set; } = "Inter";
    [BsonElement("borderRadius")]       public string  BorderRadius       { get; set; } = "8px";
    [BsonElement("darkMode")]           public bool    DarkMode           { get; set; } = false;
    [BsonElement("customCss")]          public string? CustomCss          { get; set; }
    [BsonElement("history")]            public List<TenantUiConfigSnapshot> History { get; set; } = [];
}

public class TenantUiConfigSnapshot
{
    [BsonElement("savedAt")]  public DateTime       SavedAt  { get; set; } = DateTime.UtcNow;
    [BsonElement("savedBy")]  public string         SavedBy  { get; set; } = string.Empty;
    [BsonElement("config")]   public TenantUiConfig Config   { get; set; } = new();
}

public class TenantSettings
{
    [BsonElement("currency")]           public string Currency           { get; set; } = "BRL";
    [BsonElement("locale")]             public string Locale             { get; set; } = "pt-BR";
    [BsonElement("timezone")]           public string Timezone           { get; set; } = "America/Sao_Paulo";
    [BsonElement("orderPrefix")]        public string OrderPrefix        { get; set; } = "PED";
    [BsonElement("lowStockAlert")]      public bool   LowStockAlert      { get; set; } = true;
    [BsonElement("requireMinPrice")]    public bool   RequireMinPrice    { get; set; } = true;
    [BsonElement("allowNegativeStock")] public bool   AllowNegativeStock { get; set; } = false;
}