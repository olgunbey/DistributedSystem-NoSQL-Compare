# MongoDB Replikasyon, Tutarlýlýk ve Sharding Detaylarý

Bu doküman, MongoDB'nin replikasyon yönetimi, read/write concern, read preference ve yatay sharding mekanizmalarýný örneklerle detaylý þekilde açýklar.

---

## MongoDB Replikalarý Nasýl Yönetir?

MongoDB, verileri tek bir düðüme, yani **primary** düðüme yazar. Daha sonra bu primary düðüm, deðiþiklikleri **secondary** düðümlere aktarýr ve onlarý güncel tutar.

Ancak, NoSQL veritabanlarý arasýnda replikasyon ve tutarlýlýk yaklaþýmlarý farklýlýk gösterebilir. MongoDB, **CAP Teoremi** kapsamýnda yapýlandýrýlabilir; yani kullaným senaryosuna baðlý olarak:

- **CP (Consistency + Partition Tolerance)** modu ile güçlü tutarlýlýk saðlanabilir,  
- veya **AP (Availability + Partition Tolerance)** modu ile yüksek eriþilebilirlik ön planda tutulabilir.

Bu tercihler, okuma-yazma tercihlerine (read/write concern) ve replica set ayarlarýna göre esnek þekilde yönetilir.

---

## Primary Düðüm, Secondary Düðümleri Nasýl Besler?

- Veriler öncelikle **primary** düðüme yazýlýr.  
- Yazma iþlemi baþarýlý olursa, deðiþiklikler **OPLOG (iþlem günlüðü)** adlý özel bir kayýt dosyasýna kaydedilir.  
- Eðer yapýlan iþlem veri üzerinde deðiþiklik yaratmýyorsa ya da baþarýsýz olursa, bu iþlem OPLOG’a yazýlmaz.  
- **Secondary** düðümler ise OPLOG’u sürekli izler ve burada kayýtlý iþlemleri kendi veritabanlarýna sýrayla uygular.  
- Bu süreç hýzlý olmakla birlikte tamamen anlýk deðildir; yani secondary düðümler primary düðümdeki deðiþiklikleri küçük bir gecikmeyle alýr ve günceller.  
- Böylece zamanla tüm düðümlerdeki veriler ayný duruma gelir (**eventual consistency**).

---

## Read Preference Nedir?

MongoDB, replikasyon yapýsýyla birden fazla düðüm (node) üzerinde veri tutar. Okuma iþlemlerinde hangi düðümden veri çekileceðini belirlemek için **read preference (okuma tercihi)** kullanýlýr.

Bu tercih, uygulamanýn performans, tutarlýlýk ve eriþilebilirlik ihtiyaçlarýna göre ayarlanabilir.

### Read Preference Türleri

1. **Primary (varsayýlan)**  
   Okumalar sadece primary üzerinden yapýlýr.  
   En güçlü tutarlýlýk saðlar çünkü primary'deki veri en güncel olanýdýr.

2. **Secondary**  
   Okumalar sadece secondary düðümlerden yapýlýr.  
   Primary’den farklý düðümlere yük daðýtmak için kullanýlýr.  
   Secondary düðümler, primary düðümde gerçekleþen deðiþiklikleri OPLOG üzerinden takip eder ve kendi kopyalarýna uygularlar. Eðer bir secondary düðüm, çeþitli nedenlerle OPLOG’daki güncellemeleri alamaz veya gecikmeli alýrsa, o düðümdeki veri primary ile ayný anda güncel olmaz. Bu durumda kýsa süreli tutarsýzlýk (eventual consistency) olur.

3. **primaryPreferred**  
   Öncelikle primary düðümden okuma gerçekleþir.  
   Eðer primary eriþilemiyorsa secondary düðümden okur.  
   Böylelikle yüksek eriþilebilirlik saðlanýr.

4. **secondaryPreferred**  
   Öncelikle secondary düðümlerden okumaya çalýþýr.  
   Eðer secondary düðüm yoksa ya da eriþilemiyorsa, primary düðümden okur.

5. **nearest**  
   En düþük gecikmeye sahip (network latency) düðümden okuma yapar.  
   Bu hem primary hem secondary olabilir.  
   Performans odaklý uygulamalarda kullanýlýr.

---

## Read/Write Concern Nedir?

**Write concern** ve **read concern** ayarlarýný etkili þekilde kullanarak, tutarlýlýk ve eriþilebilirlik seviyelerini ihtiyaca göre ayarlayabilirsiniz. Örneðin, daha güçlü tutarlýlýk garantileri için iþlemlerin tamamlanmasýný bekleyebilir veya tutarlýlýk gereksinimlerini gevþeterek daha yüksek eriþilebilirlik saðlayabilirsiniz.

---

## Read Concern Türleri

1. **Local**  
   Varsayýlan ayarlarda okuma yaparken, verinin çoðunluk tarafýndan kabul edilip edilmediði kontrol edilmez; bu nedenle okunan veri henüz tam olarak kalýcý olmayabilir ve ileride geri alýnabilir.  
   Orphaned doküman riski düþüktür, çünkü okuma sadece ilgili shard’daki veriye odaklanýr.

2. **Available**  
   Sharded koleksiyonlarda yetim doküman (orphaned document) dönme riski vardýr.  
   Performans odaklý, tutarlýlýktan biraz daha ödün veren senaryolarda kullanýlýr.  
   Sharded clusterlarda en düþük gecikmeli okuma saðlar.  
   En düþük tutarlýlýk seviyesidir.  
   Çok yüksek performansýn, tutarlýlýktan daha önemli olduðu özel durumlar için uygundur.

3. **Majority**  
   Sorgu, replica set üyelerinin çoðunluðu tarafýndan onaylanmýþ (acknowledged) veriyi döner.  
   Yani, okunan dokümanlar çoðunluk tarafýndan kabul edilmiþ ve kalýcý (durable) verilerdir.  
   Düzgün çalýþabilmesi için WriteConcern'in de Majority olmasý gereklidir.  
   MongoDB’de yüksek tutarlýlýk ve veri güvenliði için çoðunluk onaylý veriyi okumayý garanti eder.

4. **Linearizable**  
   MongoDB’de en yüksek tutarlýlýk garantisi veren read concern seviyesidir.  
   Bu seviye, okunan verinin, okuma iþlemi baþlamadan önce çoðunluk tarafýndan baþarýyla yazýlmýþ (acknowledged) tüm verileri içerdiðini garanti eder.  
   Burada çoðunluk onayý olduðu için writeConcern: majority olmalýdýr.  
   Yani, yapýlan okuma, çoðunlukla commit edilmiþ tüm yazma iþlemlerini yansýtýr ve kesin tutarlý (strongly consistent) bir sonuç verir.  
   Eðer yazma iþlemleri hala çoðunluða ulaþmadýysa, okuma sorgusu bu yazmalarýn çoðunluða ulaþmasýný bekleyebilir (yani sorgu, yazmalar çoðunlukta commit edilene kadar bekler).  
   Bu yüzden latency yüksek olabilir.  
   Bu, tutarlýlýk için okuma iþleminin gerektiðinde bekleyebileceði anlamýna gelir.  
   Sadece primary node üzerinde kullanýlabilir.

5. **Snapshot**  
   Snapshot read concern’in garantileri sadece transaction commit iþlemi writeConcern "majority" ile yapýldýðýnda geçerlidir.  
   Race condition sorununu önler.  
   Transactionlar arasý tutarsýzlýklar engellenir.  
   Veri bütünlüðü ve tutarlýlýðý korunur.

---

## Örnekler

### Örnek 1:  Majority kullanýmý

Elimizde 3 düðümlü (node) bir MongoDB replica set var:  
- Node1: Primary  
- Node2: Secondary  
- Node3: Secondary  

**Yazma Ýþlemi**  
Uygulama bir belgeyi (örneðin `{ userId: 1, name: "Ali" }`) Primary node’a yazar.  
Bu yazma iþlemi Node 1, 2 ve 3’ün çoðunluðu tarafýndan onaylandýðýnda (acknowledged), yani en az 2 düðümde iþlem tamamlandýðýnda, veri **majority commit point**’e ulaþýr.  
Bu noktadan sonra veri artýk kalýcý ve çoðunluk tarafýndan kabul edilmiþ olur.

**Okuma Ýþlemi (readConcern: "majority")**  
MongoDB, bu durumda veriyi, en az çoðunluðun onayladýðý ve commit ettiði en güncel haliyle döner.  
Böylece, okunan veri en az iki düðümde var olan ve onaylanmýþ veri olur.

---

### Örnek 2: Snapshot Read Concern ile Ýþlem Tutarlýlýðý

Diyelim hesap bakiyesinde 1000 TL var. Ayný anda iki iþlem gerçekleþiyor:  
- Ýþlem A: 200 TL çekme  
- Ýþlem B: 300 TL çekme  

- Ýþlem A, snapshot ile mevcut bakiye olarak 1000 TL’yi okur.  
- Ýþlem B, snapshot ile mevcut bakiye olarak 1000 TL’yi okur.  
- Ýþlem A, 200 TL çekimini yapar, ancak transaction bitmediði için diðer iþlemler bundan etkilenmez (bakiye hala 1000 TL olarak görünür).  
- Ýþlem B, 300 TL çekimini yapar, ancak transaction bitmediði için diðer iþlemler bundan etkilenmez.  
- Ýþlem A commit edilir, bakiye 800 TL olur.  
- Ýþlem B commit edilmeye çalýþýlýr ancak hata oluþur çünkü snapshot alýnýrken bakiye 1000 TL idi.  
- Bu yüzden Ýþlem B iptal edilir ve retry mekanizmasý ile tekrar denenir.

---

## Horizonal Sharding'da Veri Daðýtýmý

Diyelim elimizde büyük bir kullanýcý koleksiyonu var ve shard key olarak `userId` kullanýyoruz.  
MongoDB, `userId` aralýklarýna göre veriyi farklý shard’lara (örneðin Shard A, Shard B) yatay olarak böler.  
Her shard kendi alt kümesindeki kullanýcý verilerini tutar.

---

## Orphaned Doküman Riski Nasýl Oluþur?

- Shard A’daki bazý kullanýcýlar artýk Shard B’ye taþýnacak (chunk migration).  
- MongoDB, bu veriyi Shard B’ye kopyalar.  
- Kopyalama bittikten sonra Shard A’dan bu veriler silinir.  
- Ancak bazen að gecikmesi, kesinti ya da hata nedeniyle Shard A’daki eski veriler (artýk Shard B’ye ait olanlar) tam olarak temizlenmeyebilir.  
- Ýþte bu Shard A’daki fazla kalan dokümanlar **orphaned doküman** olur.  
- Eðer okuma sorgusu bu orphaned dokümanlarý da döndürürse, ayný veri veya eski veri birden fazla shard’dan okunabilir, bu da tutarsýzlýða yol açar.

---

## Sharding’de local ile available read concern seviyeleri arasýnda orphaned document farký nasýl ortaya çýkar?

- Elimizde iki shard olsun:  
  - Shard A: userId 1-1000 arasý kullanýcýlarý tutuyor.  
  - Shard B: userId 1001-2000 arasý kullanýcýlarý tutuyor.  

- Chunk migration ile Shard A’daki 800-1000 aralýðýndaki kullanýcýlarý Shard B’ye taþýmak istiyoruz.  
- Taþýma tamamlandýktan sonra Shard A’daki bu kullanýcýlar silinir. Ancak, network sorunlarý veya diðer aksaklýklardan dolayý Shard A’da silinmesi gereken bazý veriler kalabilir (bunlar orphaned documents olur).  

**Örnek:**  
Sorgumuz `userId = 950` olan kullanýcýyý getirmek olsun.  

- Eðer `readConcern: local` kullanýlýrsa, sorgu verinin güncel hali olan Shard B üzerinden yapýlýr. Çünkü MongoDB, shard key’e göre doðru shard’a yönlendirme yapar ve en güncel veriyi okur.  
- Ancak `readConcern: available` kullanýlýrsa, okuma mümkün olan en hýzlý shard’dan yapýlýr. Eðer sorgu Shard A’ya giderse, silinmesi gereken ve artýk geçersiz olan orphaned document okunabilir. Bu da tutarsýz veri dönme riskini artýrýr.

---

# Sonuç

MongoDB, replikasyon ve tutarlýlýk yönetimini, replica set yapýsý ile primary-secondary modeli üzerinden saðlar. Read preference ve read/write concern ayarlarýyla, uygulamanýn ihtiyaçlarýna göre tutarlýlýk ve eriþilebilirlik dengesi esnekçe yönetilir. Sharding ise yatayda veriyi daðýtarak ölçeklenebilirlik saðlar ancak doðru okuma tutarlýlýðý ayarlarý yapýlmazsa orphaned document gibi tutarsýzlýklara sebep olabilir.

---

# Kaynaklar ve Detaylar

- MongoDB Resmi Dokümantasyonu  
- CAP Teoremi açýklamalarý  
- Replica Set ve Sharding mimarileri  

---