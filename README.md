# 🔧 AI Teknik Servis Yönetim Sistemi

## 📋 Proje Hakkında

**AI Teknik Servis Yönetim Sistemi**, modern teknik servis şirketleri için geliştirilmiş kapsamlı bir web uygulamasıdır. Yapay zeka destekli analiz, otomatik iş atama, gelişmiş bildirim sistemi ve detaylı raporlama özellikleri ile teknik servis süreçlerini optimize eder.

## ✨ Temel Özellikler

### 🤖 Yapay Zeka Entegrasyonu
- **Google Gemini AI** ile otomatik servis talebi analizi
- Akıllı kategori ve öncelik tahmini
- Teknisyen önerisi ve iş yükü optimizasyonu
- AI destekli rapor analizi ve öneriler
- Tahmin doğruluğu ve güven skoru takibi

### 👥 Çok Katmanlı Kullanıcı Yönetimi
- **Müşteri Paneli**: Servis talebi oluşturma, takip, geçmiş görüntüleme
- **Teknisyen Paneli**: Atanan görevler, durum güncelleme, zaman takibi
- **Yönetici Paneli**: İş atama, performans takibi, ekip yönetimi
- **Admin Paneli**: Sistem yönetimi, kullanıcı yönetimi, konfigürasyon

### 📧 Gelişmiş Bildirim Sistemi
- HTML email template'leri ile profesyonel bildirimler
- Otomatik durum güncellemeleri
- Acil durum bildirimleri
- Email doğrulama sistemi
- Bildirim geçmişi ve takibi

### 📊 Kapsamlı Raporlama
- Detaylı performans raporları
- Teknisyen iş yükü analizi
- Kategori bazında istatistikler
- AI destekli trend analizi
- PDF rapor oluşturma

### 🔄 İş Akışı Yönetimi
- Otomatik iş atama algoritması
- Teknisyen müsaitlik takibi
- Öncelik bazlı görev sıralama
- Esnek durum yönetimi
- Zaman ve maliyet takibi

## 🛠️ Teknoloji Stack

### Backend
- **Framework**: ASP.NET Core 8.0
- **ORM**: Entity Framework Core
- **Veritabanı**: SQL Server
- **Authentication**: ASP.NET Core Identity
- **Mapping**: AutoMapper
- **AI Service**: Google Gemini API

### Frontend
- **UI Framework**: Bootstrap 5
- **JavaScript**: Vanilla JS + jQuery
- **Charts**: Chart.js
- **Icons**: Font Awesome
- **Responsive Design**: Mobile-first approach

### Altyapı
- **Email Service**: SMTP (Gmail entegrasyonu)
- **File Management**: Local file storage
- **Logging**: ASP.NET Core Logging
- **Configuration**: appsettings.json
- **Dependency Injection**: Built-in DI Container

## 🏗️ Proje Mimarisi

```
AiTeknikServis/
├── Controllers/           # MVC Controllers
│   ├── CustomerController.cs
│   ├── TechnicianController.cs
│   ├── ManagerController.cs
│   └── ServiceRequestController.cs
├── Areas/
│   └── Admin/            # Admin area
├── Services/             # Business Logic
│   ├── ServiceRequestService.cs
│   ├── NotificationService.cs
│   ├── WorkAssignmentService.cs
│   └── ReportService.cs
├── Infrastructure/       # Infrastructure Services
│   ├── AI/
│   │   └── GeminiAiService.cs
│   ├── Notifications/
│   │   └── EmailService.cs
│   └── Extensions/
├── Repositories/         # Data Access Layer
├── Entities/            # Domain Models & DTOs
├── Data/                # DbContext
├── Templates/           # Email Templates
└── Views/               # Razor Views
```

## 🚀 Kurulum ve Çalıştırma

### Gereksinimler
- .NET 8.0 SDK
- SQL Server (LocalDB veya tam sürüm)
- Visual Studio 2022 veya VS Code
- Git

### Adım Adım Kurulum

1. **Projeyi klonlayın**
   ```bash
   git clone [repository-url]
   cd AiTeknikServis
   ```

2. **Bağımlılıkları yükleyin**
   ```bash
   dotnet restore
   ```

3. **Veritabanı bağlantısını yapılandırın**
   ```json
   // appsettings.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=.;Initial Catalog=AiTeknikServis;Integrated Security=True;Trust Server Certificate=True"
     }
   }
   ```

4. **Email ayarlarını yapılandırın**
   ```json
   // appsettings.json
   {
     "EmailSettings": {
       "SmtpHost": "smtp.gmail.com",
       "SmtpPort": 587,
       "SmtpUsername": "your-email@gmail.com",
       "SmtpPassword": "your-app-password",
       "FromEmail": "your-email@gmail.com",
       "FromName": "AI TechServ"
     }
   }
   ```

5. **AI API anahtarını ekleyin**
   ```json
   // appsettings.json
   {
     "AiSettings": {
       "GeminiApiKey": "your-gemini-api-key",
       "Enabled": true
     }
   }
   ```

6. **Veritabanını oluşturun**
   ```bash
   dotnet ef database update
   ```

7. **Uygulamayı çalıştırın**
   ```bash
   dotnet run
   ```

8. **Tarayıcıda açın**
   ```
   https://localhost:5001
   ```

## 👤 Varsayılan Kullanıcılar

Uygulama ilk çalıştırıldığında otomatik olarak aşağıdaki kullanıcılar oluşturulur:

- **Admin**: admin@aiteknikservis.com / Admin123!
- **Roller**: Admin, Manager, Technician, Customer

## 📱 Kullanım Kılavuzu

### Müşteri İşlemleri
1. Hesap oluşturma ve email doğrulama
2. Servis talebi oluşturma
3. Dosya ekleme ve açıklama yazma
4. Talep durumu takibi
5. Geçmiş talepleri görüntüleme

### Teknisyen İşlemleri
1. Atanan görevleri görüntüleme
2. Görev detaylarını inceleme
3. Durum güncelleme
4. Çözüm notları ekleme
5. Tamamlama işlemleri

### Yönetici İşlemleri
1. Tüm talepleri görüntüleme
2. Teknisyen atama
3. İş yükü analizi
4. Performans raporları
5. Acil durum yönetimi

### Admin İşlemleri
1. Kullanıcı yönetimi
2. Sistem konfigürasyonu
3. Email template yönetimi
4. AI ayarları
5. Sistem raporları

## 🔧 Konfigürasyon

### AI Ayarları
```json
{
  "AiSettings": {
    "Enabled": true,
    "ConfidenceThreshold": 0.7,
    "MaxRetries": 3,
    "GeminiApiKey": "your-api-key",
    "GeminiApiUrl": "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent"
  }
}
```

### Email Doğrulama
```json
{
  "EmailVerification": {
    "DailyLimit": 10,
    "RateLimitWindowMinutes": 60,
    "MaxRequestsPerWindow": 5,
    "DefaultExpiryMinutes": 15
  }
}
```

## 📈 Performans Özellikleri

- **Asenkron İşlemler**: Tüm veritabanı işlemleri async/await pattern
- **Caching**: Email template ve AI sonuçları cache'leme
- **Optimized Queries**: EF Core ile optimize edilmiş sorgular
- **Connection Pooling**: Veritabanı bağlantı havuzu
- **Lazy Loading**: İhtiyaç duyulan veriler lazy loading

## 🔒 Güvenlik

- **Authentication**: ASP.NET Core Identity
- **Authorization**: Role-based access control
- **Data Protection**: Hassas verilerin şifrelenmesi
- **Input Validation**: Tüm girişlerde doğrulama
- **SQL Injection Protection**: Parameterized queries
- **XSS Protection**: Output encoding

## 🧪 Test Edilmiş Özellikler

- ✅ Kullanıcı kayıt ve giriş işlemleri
- ✅ Servis talebi oluşturma ve yönetimi
- ✅ AI destekli analiz ve tahminler
- ✅ Email bildirim sistemi
- ✅ Dosya yükleme ve yönetimi
- ✅ Raporlama ve istatistikler
- ✅ Responsive tasarım

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add amazing feature'`)
4. Branch'inizi push edin (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için `LICENSE` dosyasına bakın.

## 📞 İletişim

Proje hakkında sorularınız için:
- Email: aitechnicalserv@gmail.com
- GitHub Issues: [Sorun bildirin](https://github.com/your-repo/issues)

## 🙏 Teşekkürler

- **Google Gemini AI** - Yapay zeka servisleri
- **Microsoft** - .NET Core framework
- **Bootstrap Team** - UI framework
- **Chart.js** - Grafik kütüphanesi

---

**AI Teknik Servis Yönetim Sistemi** ile teknik servis süreçlerinizi modernize edin! 🚀
