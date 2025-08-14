# MongoDB Replikasyon, Tutarlılık ve Sharding Detayları

Bu doküman, MongoDB'nin replikasyon yönetimi, read/write concern, read preference ve yatay sharding mekanizmalarını örneklerle detaylı şekilde açıklar.

---

## MongoDB Replikaları Nasıl Yönetir?

MongoDB, verileri tek bir düğüme, yani **primary** düğüme yazar. Daha sonra bu primary düğüm, değişiklikleri **OPLOG** aracılığı ile  **secondary** düğümlere aktarır ve onları güncel tutar.

Ancak, NoSQL veritabanları arasında replikasyon ve tutarlılık yaklaşımları farklılık gösterebilir. MongoDB, **CAP Teoremi** kapsamında yapılandırılabilir; yani kullanım senaryosuna bağlı olarak:

- **CP (Consistency + Partition Tolerance)** modu ile güçlü tutarlılık sağlanabilir,  
- veya **AP (Availability + Partition Tolerance)** modu ile yüksek erişilebilirlik ön planda tutulabilir.

Bu tercihler, okuma-yazma tercihlerine (read/write concern) ve replica set ayarlarına göre esnek şekilde yönetilir.

---



## Primary Düğüm, Secondary Düğümleri Nasıl Besler?

- Veriler öncelikle **primary** düğüme yazılır.  
- Yazma işlemi başarılı olursa, değişiklikler **OPLOG (işlem günlüğü)** adlı özel bir kayıt dosyasına kaydedilir.  
- Eğer yapılan işlem veri üzerinde değişiklik yaratmıyorsa ya da başarısız olursa, bu işlem OPLOG’a yazılmaz.  
- **Secondary** düğümler ise OPLOG’u sürekli izler ve burada kayıtlı işlemleri kendi veritabanlarına sırayla uygular.  
- Bu süreç hızlı olmakla birlikte tamamen anlık değildir; yani secondary düğümler primary düğümdeki değişiklikleri küçük bir gecikmeyle alır ve günceller.  
- Böylece zamanla tüm düğümlerdeki veriler aynı duruma gelir (**eventual consistency**).

---

## WriteConcern Nedir?
MongoDB’de WriteConcern, bir yazma işleminin ne kadar güvenli ve garanti edilmiş şekilde tamamlanacağını belirleyen mekanizmadır.
Bu, verinin kaç düğüme yazıldıktan sonra "başarılı" kabul edileceğini tanımlar.
- Varsayılan olarak w parametresi majority (çoğunluk) değerindedir. Bu, replika setindeki düğümlerin çoğunluğu yazma işlemini onaylamadan işlem tamamlanmış sayılmaz demektir.
- Örneğin elimizde 5 düğüm (1 Primary, 4 Secondary) varsa, w: majority demek en az 3 düğümün yazmayı onaylaması gerektiği anlamına gelir.
- Eğer w: 2 yazarsak, çoğunluk yerine en az 2 düğüme yazıldığında işlem başarılı kabul edilir. Bu, yazma işlemini hızlandırabilir ancak veri kaybı riskini artırır.
- Sistem failover durumuna geçerse  w: majority seçilmesi rollback riskini azaltır.
- Eğer sistemde commit majority point hesaplanırsa rollback riski komple ortadan kalkar.

---
## MongoDB: Çoğunluk Kaybında Tüm Düğümlerin Secondary Olması
- MongoDB replikasında primary seçimi için çoğunluk (majority) sağlanmalıdır. Örneğin 5 düğümlü bir kümede en az 3 düğüm ayakta olmalıdır.
- Eğer çoğunluk sağlanmazsa, mevcut primary dahil tüm düğümler secondary moduna geçer. Bu durumda yazma işlemleri durur.
- Varsayılan Read Preference değeri primary olduğu için okuma da yapılamaz.
- Okumaya devam edebilmek için Read Preference değerini secondary veya secondaryPreferred olarak değiştirmek gerekir.


## Read Preference Nedir?

MongoDB, replikasyon yapısıyla birden fazla düğüm (node) üzerinde veri tutar. Okuma işlemlerinde hangi düğümden veri çekileceğini belirlemek için **read preference (okuma tercihi)** kullanılır.

Bu tercih, uygulamanın performans, tutarlılık ve erişilebilirlik ihtiyaçlarına göre ayarlanabilir.

### Read Preference Türleri

1. **Primary (varsayılan)**  
   Okumalar sadece primary üzerinden yapılır.  
   En güçlü tutarlılık sağlar çünkü primary'deki veri en güncel olanıdır.

2. **Secondary**  
   Okumalar sadece secondary düğümlerden yapılır.  
   Primary’den farklı düğümlere yük dağıtmak için kullanılır.  
   Secondary düğümler, primary düğümde gerçekleşen değişiklikleri OPLOG üzerinden takip eder ve kendi kopyalarına uygularlar. Eğer bir secondary düğüm, çeşitli nedenlerle OPLOG’daki güncellemeleri alamaz veya gecikmeli alırsa, o düğümdeki veri primary ile aynı anda güncel olmaz. Bu durumda kısa süreli tutarsızlık (eventual consistency) olur.

3. **primaryPreferred**  
   Öncelikle primary düğümden okuma gerçekleşir.  
   Eğer primary erişilemiyorsa secondary düğümden okur.  
   Böylelikle yüksek erişilebilirlik sağlanır.

4. **secondaryPreferred**  
   Öncelikle secondary düğümlerden okumaya çalışır.  
   Eğer secondary düğüm yoksa ya da erişilemiyorsa, primary düğümden okur.

5. **nearest**  
   En düşük gecikmeye sahip (network latency) düğümden okuma yapar.  
   Bu hem primary hem secondary olabilir.  
   Performans odaklı uygulamalarda kullanılır.

---

## Read/Write Concern Nedir?

**Write concern** ve **read concern** ayarlarını etkili şekilde kullanarak, tutarlılık ve erişilebilirlik seviyelerini ihtiyaca göre ayarlayabilirsiniz. Örneğin, daha güçlü tutarlılık garantileri için işlemlerin tamamlanmasını bekleyebilir veya tutarlılık gereksinimlerini gevşeterek daha yüksek erişilebilirlik sağlayabilirsiniz.

---

## Read Concern Türleri

1. **Local**  
   Varsayılan ayarlarda okuma yaparken, verinin çoğunluk tarafından kabul edilip edilmediği kontrol edilmez; bu nedenle okunan veri henüz tam olarak kalıcı olmayabilir ve ileride geri alınabilir.  
   Orphaned doküman riski düşüktür, çünkü okuma sadece ilgili shard’daki veriye odaklanır.

2. **Available**  
   Sharded koleksiyonlarda yetim doküman (orphaned document) dönme riski vardır.  
   Performans odaklı, tutarlılıktan biraz daha ödün veren senaryolarda kullanılır.  
   Sharded clusterlarda en düşük gecikmeli okuma sağlar.  
   En düşük tutarlılık seviyesidir.  
   Çok yüksek performansın, tutarlılıktan daha önemli olduğu özel durumlar için uygundur.

3. **Majority**  
   Sorgu, replica set üyelerinin çoğunluğu tarafından onaylanmış (acknowledged) veriyi döner.  
   Yani, okunan dokümanlar çoğunluk tarafından kabul edilmiş ve kalıcı (durable) verilerdir.  
   MongoDB’de yüksek tutarlılık ve veri güvenliği için çoğunluk onaylı veriyi okumayı garanti eder.

4. **Linearizable**  
   MongoDB’de en yüksek tutarlılık garantisi veren read concern seviyesidir.  
   Bu seviye, okunan verinin, okuma işlemi başlamadan önce çoğunluk tarafından başarıyla yazılmış (acknowledged) tüm verileri içerdiğini garanti eder.   
   Yani, yapılan okuma, çoğunlukla commit edilmiş tüm yazma işlemlerini yansıtır ve kesin tutarlı (strongly consistent) bir sonuç verir.  
   Eğer yazma işlemleri hala çoğunluğa ulaşmadıysa, okuma sorgusu bu yazmaların çoğunluğa ulaşmasını bekleyebilir (yani sorgu, yazmalar çoğunlukta commit edilene kadar bekler).  
   Bu yüzden latency yüksek olabilir.  
   Bu, tutarlılık için okuma işleminin gerektiğinde bekleyebileceği anlamına gelir.  
   Sadece primary node üzerinde kullanılabilir.

5. **Snapshot**  
   Snapshot read concern’in garantileri sadece transaction commit işlemi writeConcern "majority" ile yapıldığında geçerlidir.  
   Race condition sorununu önler.  
   Transactionlar arası tutarsızlıklar engellenir.  
   Veri bütünlüğü ve tutarlılığı korunur.

---

## Örnekler

### Örnek 1:  Majority kullanımı

Elimizde 3 düğümlü (node) bir MongoDB replica set var:  
- Node1: Primary  
- Node2: Secondary  
- Node3: Secondary  

**Yazma İşlemi**  
Uygulama bir belgeyi (örneğin `{ userId: 1, name: "Ali" }`) Primary node’a yazar.  
Bu yazma işlemi Node 1, 2 ve 3’ün çoğunluğu tarafından onaylandığında (acknowledged), yani en az 2 düğümde işlem tamamlandığında, veri **majority commit point**’e ulaşır.  
Bu noktadan sonra veri artık kalıcı ve çoğunluk tarafından kabul edilmiş olur.

**Okuma İşlemi (readConcern: "majority")**  
MongoDB, bu durumda veriyi, en az çoğunluğun onayladığı ve commit ettiği en güncel haliyle döner.  
Böylece, okunan veri en az iki düğümde var olan ve onaylanmış veri olur.

---

### Örnek 2: Snapshot Read Concern ile İşlem Tutarlılığı

Diyelim hesap bakiyesinde 1000 TL var. Aynı anda iki işlem gerçekleşiyor:  
- İşlem A: 200 TL çekme  
- İşlem B: 300 TL çekme  

- İşlem A, snapshot ile mevcut bakiye olarak 1000 TL’yi okur.  
- İşlem B, snapshot ile mevcut bakiye olarak 1000 TL’yi okur.  
- İşlem A, 200 TL çekimini yapar, ancak transaction bitmediği için diğer işlemler bundan etkilenmez (bakiye hala 1000 TL olarak görünür).  
- İşlem B, 300 TL çekimini yapar, ancak transaction bitmediği için diğer işlemler bundan etkilenmez.  
- İşlem A commit edilir, bakiye 800 TL olur.  
- İşlem B commit edilmeye çalışılır ancak hata oluşur çünkü snapshot alınırken bakiye 1000 TL idi.  
- Bu yüzden İşlem B iptal edilir ve retry mekanizması ile tekrar denenir.

---

## Horizonal Sharding'da Veri Dağıtımı

Diyelim elimizde büyük bir kullanıcı koleksiyonu var ve shard key olarak `userId` kullanıyoruz.  
MongoDB, `userId` aralıklarına göre veriyi farklı shard’lara (örneğin Shard A, Shard B) yatay olarak böler.  
Her shard kendi alt kümesindeki kullanıcı verilerini tutar.

---

## Orphaned Doküman Riski Nasıl Oluşur?

- Shard A’daki bazı kullanıcılar artık Shard B’ye taşınacak (chunk migration).  
- MongoDB, bu veriyi Shard B’ye kopyalar.  
- Kopyalama bittikten sonra Shard A’dan bu veriler silinir.  
- Ancak bazen ağ gecikmesi, kesinti ya da hata nedeniyle Shard A’daki eski veriler (artık Shard B’ye ait olanlar) tam olarak temizlenmeyebilir.  
- İşte bu Shard A’daki fazla kalan dokümanlar **orphaned doküman** olur.  
- Eğer okuma sorgusu bu orphaned dokümanları da döndürürse, aynı veri veya eski veri birden fazla shard’dan okunabilir, bu da tutarsızlığa yol açar.
- Sistem failover olduğu zaman orphaned veri oluşma ihtimali vardır.
---

## Sharding’de local ile available read concern seviyeleri arasında orphaned document farkı nasıl ortaya çıkar?

- Elimizde iki shard olsun:  
  - Shard A: userId 1-1000 arası kullanıcıları tutuyor.  
  - Shard B: userId 1001-2000 arası kullanıcıları tutuyor.  

- Chunk migration ile Shard A’daki 800-1000 aralığındaki kullanıcıları Shard B’ye taşımak istiyoruz.  
- Taşıma tamamlandıktan sonra Shard A’daki bu kullanıcılar silinir. Ancak, network sorunları veya diğer aksaklıklardan dolayı Shard A’da silinmesi gereken bazı veriler kalabilir (bunlar orphaned documents olur).  

**Örnek:**  
Sorgumuz `userId = 950` olan kullanıcıyı getirmek olsun.  

- Sorgu, config server'daki chunk bilgisine göre doğru shard'a (Shard B) yönlendirilir ve o shard'ın en güncel (committed veya orphaned olmayan) verisini okur.
- Ancak `readConcern: available` kullanılırsa, okuma mümkün olan en hızlı shard’dan yapılır. Eğer sorgu Shard A’ya giderse, silinmesi gereken ve artık geçersiz olan orphaned document okunabilir. Bu da tutarsız veri dönme riskini artırır.

---

# Sonuç

MongoDB, replikasyon ve tutarlılık yönetimini, replica set yapısı ile primary-secondary modeli üzerinden sağlar. Read preference ve read/write concern ayarlarıyla, uygulamanın ihtiyaçlarına göre tutarlılık ve erişilebilirlik dengesi esnekçe yönetilir. Sharding ise yatayda veriyi dağıtarak ölçeklenebilirlik sağlar ancak doğru okuma tutarlılığı ayarları yapılmazsa orphaned document gibi tutarsızlıklara sebep olabilir.

---

# Kaynaklar ve Detaylar

- MongoDB Resmi Dokümantasyonu  
- CAP Teoremi açıklamaları  
- Replica Set ve Sharding mimarileri  

---

