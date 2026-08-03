<div align="center">

# SmartQRMenu

### _Akıllı Restoran, QR Menü ve Yapay Zekâ Destekli Restoran Platformu_

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker)
![MS SQL Server](https://img.shields.io/badge/MSSQL-2022-CC292B?style=for-the-badge&logo=microsoftsqlserver)
![Semantic Kernel](https://img.shields.io/badge/AI-Semantic%20Kernel-blue?style=for-the-badge)

_Microsoft Yaz Kampı projesi olarak N-Tier Architecture prensiplerine uygun geliştirilmiş, Docker destekli, RAG tabanlı akıllı restoran platformu._

</div>

 <h1>Proje Hakkında</h1>

**SmartQRMenu AI**, geleneksel restoran menülerini yapay zekâ destekli bir müşteri deneyimine dönüştüren web platformudur. Kullanıcılar QR kodlar üzerinden dinamik menülere erişebilir, ürün yorumları ve değerlendirmeleri yapabilir.

Sistem arka planda yerel LLM modellerini (**Ollama / Llama 3**) ve **Semantic Kernel** mimarisini kullanarak müşterilerin sorularına restoran verilerine RAG dayalı akıllı yanıtlar sunar.

 <h1>Hızlı Başlangıç </h1>

Proje, herhangi bir ek bağımlılıkların (MS SQL Server veya LLM) manuel olarak kurulmasına gerek kalmadan **tek komutla** çalışacak şekilde Docker ile konteynerize edilmiştir.

### <h1>Ön Koşullar</h1>

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) 'ın bilgisayarınızda çalışıyor olması yeterlidir.

 <h1>Çalıştırma Adımları</h1>

 <h3>1. Repoyu klonlayın:</h3>
   <br>
   <ul>
      <li>git clone https://github.com/AlperOlg/SmartMenu.git SmartQRMenu</li>
      <li>cd SmartQRMenu</li>
   </ul>

   <h3>2. Tüm mimariyi (Web API/MVC, MSSQL, Ollama) başlatın:</h3>
   <ul>
      <li>docker compose up --build -d</li>
      <br>
      <li>Web Arayüzü: http://localhost:5000</li>
       <h6><i>Uygulama ilk kez ayağa kalkarken EF Core migration'ları ve başlangıç verileri otomatik olarak işlenir.</i></h6>
      <br>
      <li>Ollama llama3 port: http://localhost:11434 </li>
      <h6><i>(llama3 modeli çalışıyor mu diye kontrol edebilirsiniz.)</i></h6>
   </ul>

   <h1>Teknik Yığın (Tech Stack)</h1>
   <h3>Backend & Architecture</h3>
   <ul>
    <li><b>Framework: </b>.NET 9.0 (ASP.NET Core MVC)</li>
    <li><b>Mimari: </b>N-Tier Architecture (Core, DataAccess, Business, Web)</li>
    <li><b>ORM & Database: </b>Entity Framework Core, MS SQL Server 2022</li>
    <li><b>Yapay Zekâ (AI & RAG): </b>Microsoft Semantic Kernel, Ollama (Llama 3)</li>
    <li><b>QR Üretimi: </b>QRCoder</li>
    <li><b>DevOps: </b>Docker</li>
   </ul>
   <h3>Frontend</h3>
   <ul>
      <li>Razor Pages</li>
      <li>AJAX</li>
      <li>JavaScript</li>
      <li>FontAwesome</li>
      <li>Bootstrap 5</li>
      <li>Howler.js</li>
   </ul>
    <hr/>

   <h1>Ekran Görüntüleri</h1>
 <h1>Ana Ekran</h1>
<img width="1421" height="1012" alt="image" src="https://github.com/user-attachments/assets/03ade4ee-1ef8-4222-ac05-d218193c6243" />
        
<h1>Giriş Ekranı</h1>
<img width="685" height="829" alt="image" src="https://github.com/user-attachments/assets/fb99791e-d8e1-45ec-abe4-8b133a421345" />
        
<h1>Kayıt Ekranı</h1>
 <img width="576" height="877" alt="image" src="https://github.com/user-attachments/assets/831a5195-5f8d-4f0b-9276-73889a0523ce" />
        
 <h1>AI Chat Ekranı</h1>
<img width="1432" height="608" alt="image" src="https://github.com/user-attachments/assets/4964fe2f-8489-47b4-ad51-b66eff4aa3d6" />

<h1>Restoran Panelleri</h1>
<img width="1403" height="536" alt="image" src="https://github.com/user-attachments/assets/62424100-30e8-415b-a59d-46affc3cb932" />
<img width="1360" height="1250" alt="image" src="https://github.com/user-attachments/assets/76343b7e-ed51-4dcf-ac6b-2d36411b770f" />
<img width="1331" height="1014" alt="image" src="https://github.com/user-attachments/assets/6c89588d-560f-4626-9222-19387f1353dc" />

   <h1>TODO:</h1>
      <h3>[x] Yapay zekâ sohbet geçmişi (Chat History) entegrasyonu</h3>
      <h3>[x] Ses efektleri ve bildirim sistemleri</h3>
      <h3>[x] Admin ve Employee rol tabanlı yetkilendirme</h3>
      <h3>[x] Vector Store RAG mimarisi ve DB senkronizasyonu</h3>
      <h3>[x] Dinamik Malzeme (Ingredient/MenuIngredient) yönetimi</h3>
      <h3>[x] 2FA şartlı restoran oluşturma mekanizması</h3>
      <h3>[x] Dockerizasyon </h3>
      <h3>[ ] Çoklu dil desteği</h3>
   <br>
