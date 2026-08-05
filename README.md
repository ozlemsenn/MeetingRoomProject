#  Kurumsal Toplantı Odası Rezervasyon Sistemi

Bu proje, kurumların veya ortak paylaşımlı ofis alanlarının toplantı odalarını verimli bir şekilde yönetebilmesi için geliştirilmiş, **çoklu şirket (multi-tenant)** destekli ve rol tabanlı bir ASP.NET MVC web uygulamasıdır. 

Yazılım stajım kapsamında, uçtan uca mimari tasarımı, veritabanı ilişkileri ve ön-yüz (UI) entegrasyonları tarafımca geliştirilmiştir.

##  Temel Özellikler

- **Rol Tabanlı Yetkilendirme (Role-Based Auth):**
  - **Admin:** Tüm sistemi, şirketleri, odaları ve kullanıcıları yönetebilir.
  - **Yönetici:** Sadece kendi şirketine ait personelleri, odaları ve rezervasyonları yönetebilir (Gelişmiş Veri İzolasyonu).
  - **Personel:** Yalnızca kendi şirketindeki odaları görüntüleyip rezervasyon oluşturabilir.

- **Akıllı Rezervasyon ve Çakışma Kontrolü:**
  - Aynı odaya, aynı saat aralığında birden fazla rezervasyon yapılması algoritma ile engellenmiştir.
  - **Güvenlik Kilidi:** Toplantı başlangıcına 1 saatten az süre kalan rezervasyonlar kilitlenir; iptal edilemez ve düzenlenemez.
  - Geçmiş tarihe veya geçmiş saate toplantı oluşturulması engellenmiştir.

- **Asenkron Kullanıcı Deneyimi (AJAX Modals):**
  - Kullanıcı, Oda ve Rezervasyon modüllerindeki tüm Ekle/Sil/Güncelle işlemleri (CRUD) sayfayı yenilemeden (AJAX & jQuery) dinamik modallar üzerinden gerçekleşir.
  - Veri silme işlemleri sırasında "Yumuşak Silme (Soft Delete) / Pasife Alma" mantığı kurgulanmış olup, pasif nedenleri sistemde tutulmaktadır.

- **Dinamik Raporlama:**
  - `ClosedXML` kütüphanesi kullanılarak toplantı verileri saniyeler içinde formatlı Excel (.xlsx) raporlarına dönüştürülebilir.

##  Kullanılan Teknolojiler

**Backend (Sunucu Tarafı):**
- C# & ASP.NET MVC
- Entity Framework (ORM)
- LINQ
- SQL Server

**Frontend (İstemci Tarafı):**
- HTML5, CSS3, Bootstrap 5
- JavaScript, jQuery, AJAX
- SweetAlert2 (Kullanıcı Dostu Bildirimler)
- DataTables (Asenkron Tablo Yönetimi ve Filtreleme)

##  Ekran Görüntüleri
<img width="1028" height="958" alt="Ekran görüntüsü 2026-08-05 111523" src="https://github.com/user-attachments/assets/a0e18311-2e4c-4788-9f47-abfda15cbba0" />
<img width="1902" height="905" alt="Ekran görüntüsü 2026-08-05 111549" src="https://github.com/user-attachments/assets/7aafdb1d-0d4f-44b5-9a0e-e773986ac214" />
<img width="886" height="928" alt="Ekran görüntüsü 2026-08-05 111601" src="https://github.com/user-attachments/assets/950c47e8-5a36-4fba-beae-60a508fe0f2c" />
<img width="1553" height="800" alt="Ekran görüntüsü 2026-08-05 111614" src="https://github.com/user-attachments/assets/5d6de4dc-3462-4acc-b1fd-6498a19243ca" />
<img width="1561" height="772" alt="Ekran görüntüsü 2026-08-05 111625" src="https://github.com/user-attachments/assets/241918ec-aeef-4ec3-9a79-9acb3357ee97" />



##  Kurulum ve Çalıştırma

1. Projeyi bilgisayarınıza klonlayın:
   ```bash
   git clone [https://github.com/ozlemsenn/MeetingProject.git](https://github.com/ozlemsenn/MeetingProject.git)
