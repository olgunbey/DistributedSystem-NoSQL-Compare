# MongoDB Replikasyon, Tutarl�l�k ve Sharding Detaylar�

Bu dok�man, MongoDB'nin replikasyon y�netimi, read/write concern, read preference ve yatay sharding mekanizmalar�n� �rneklerle detayl� �ekilde a��klar.

---

## MongoDB Replikalar� Nas�l Y�netir?

MongoDB, verileri tek bir d���me, yani **primary** d���me yazar. Daha sonra bu primary d���m, de�i�iklikleri **secondary** d���mlere aktar�r ve onlar� g�ncel tutar.

Ancak, NoSQL veritabanlar� aras�nda replikasyon ve tutarl�l�k yakla��mlar� farkl�l�k g�sterebilir. MongoDB, **CAP Teoremi** kapsam�nda yap�land�r�labilir; yani kullan�m senaryosuna ba�l� olarak:

- **CP (Consistency + Partition Tolerance)** modu ile g��l� tutarl�l�k sa�lanabilir,  
- veya **AP (Availability + Partition Tolerance)** modu ile y�ksek eri�ilebilirlik �n planda tutulabilir.

Bu tercihler, okuma-yazma tercihlerine (read/write concern) ve replica set ayarlar�na g�re esnek �ekilde y�netilir.

---

## Primary D���m, Secondary D���mleri Nas�l Besler?

- Veriler �ncelikle **primary** d���me yaz�l�r.  
- Yazma i�lemi ba�ar�l� olursa, de�i�iklikler **OPLOG (i�lem g�nl���)** adl� �zel bir kay�t dosyas�na kaydedilir.  
- E�er yap�lan i�lem veri �zerinde de�i�iklik yaratm�yorsa ya da ba�ar�s�z olursa, bu i�lem OPLOG�a yaz�lmaz.  
- **Secondary** d���mler ise OPLOG�u s�rekli izler ve burada kay�tl� i�lemleri kendi veritabanlar�na s�rayla uygular.  
- Bu s�re� h�zl� olmakla birlikte tamamen anl�k de�ildir; yani secondary d���mler primary d���mdeki de�i�iklikleri k���k bir gecikmeyle al�r ve g�nceller.  
- B�ylece zamanla t�m d���mlerdeki veriler ayn� duruma gelir (**eventual consistency**).

---

## Read Preference Nedir?

MongoDB, replikasyon yap�s�yla birden fazla d���m (node) �zerinde veri tutar. Okuma i�lemlerinde hangi d���mden veri �ekilece�ini belirlemek i�in **read preference (okuma tercihi)** kullan�l�r.

Bu tercih, uygulaman�n performans, tutarl�l�k ve eri�ilebilirlik ihtiya�lar�na g�re ayarlanabilir.

### Read Preference T�rleri

1. **Primary (varsay�lan)**  
   Okumalar sadece primary �zerinden yap�l�r.  
   En g��l� tutarl�l�k sa�lar ��nk� primary'deki veri en g�ncel olan�d�r.

2. **Secondary**  
   Okumalar sadece secondary d���mlerden yap�l�r.  
   Primary�den farkl� d���mlere y�k da��tmak i�in kullan�l�r.  
   Secondary d���mler, primary d���mde ger�ekle�en de�i�iklikleri OPLOG �zerinden takip eder ve kendi kopyalar�na uygularlar. E�er bir secondary d���m, �e�itli nedenlerle OPLOG�daki g�ncellemeleri alamaz veya gecikmeli al�rsa, o d���mdeki veri primary ile ayn� anda g�ncel olmaz. Bu durumda k�sa s�reli tutars�zl�k (eventual consistency) olur.

3. **primaryPreferred**  
   �ncelikle primary d���mden okuma ger�ekle�ir.  
   E�er primary eri�ilemiyorsa secondary d���mden okur.  
   B�ylelikle y�ksek eri�ilebilirlik sa�lan�r.

4. **secondaryPreferred**  
   �ncelikle secondary d���mlerden okumaya �al���r.  
   E�er secondary d���m yoksa ya da eri�ilemiyorsa, primary d���mden okur.

5. **nearest**  
   En d���k gecikmeye sahip (network latency) d���mden okuma yapar.  
   Bu hem primary hem secondary olabilir.  
   Performans odakl� uygulamalarda kullan�l�r.

---

## Read/Write Concern Nedir?

**Write concern** ve **read concern** ayarlar�n� etkili �ekilde kullanarak, tutarl�l�k ve eri�ilebilirlik seviyelerini ihtiyaca g�re ayarlayabilirsiniz. �rne�in, daha g��l� tutarl�l�k garantileri i�in i�lemlerin tamamlanmas�n� bekleyebilir veya tutarl�l�k gereksinimlerini gev�eterek daha y�ksek eri�ilebilirlik sa�layabilirsiniz.

---

## Read Concern T�rleri

1. **Local**  
   Varsay�lan ayarlarda okuma yaparken, verinin �o�unluk taraf�ndan kabul edilip edilmedi�i kontrol edilmez; bu nedenle okunan veri hen�z tam olarak kal�c� olmayabilir ve ileride geri al�nabilir.  
   Orphaned dok�man riski d���kt�r, ��nk� okuma sadece ilgili shard�daki veriye odaklan�r.

2. **Available**  
   Sharded koleksiyonlarda yetim dok�man (orphaned document) d�nme riski vard�r.  
   Performans odakl�, tutarl�l�ktan biraz daha �d�n veren senaryolarda kullan�l�r.  
   Sharded clusterlarda en d���k gecikmeli okuma sa�lar.  
   En d���k tutarl�l�k seviyesidir.  
   �ok y�ksek performans�n, tutarl�l�ktan daha �nemli oldu�u �zel durumlar i�in uygundur.

3. **Majority**  
   Sorgu, replica set �yelerinin �o�unlu�u taraf�ndan onaylanm�� (acknowledged) veriyi d�ner.  
   Yani, okunan dok�manlar �o�unluk taraf�ndan kabul edilmi� ve kal�c� (durable) verilerdir.  
   D�zg�n �al��abilmesi i�in WriteConcern'in de Majority olmas� gereklidir.  
   MongoDB�de y�ksek tutarl�l�k ve veri g�venli�i i�in �o�unluk onayl� veriyi okumay� garanti eder.

4. **Linearizable**  
   MongoDB�de en y�ksek tutarl�l�k garantisi veren read concern seviyesidir.  
   Bu seviye, okunan verinin, okuma i�lemi ba�lamadan �nce �o�unluk taraf�ndan ba�ar�yla yaz�lm�� (acknowledged) t�m verileri i�erdi�ini garanti eder.  
   Burada �o�unluk onay� oldu�u i�in writeConcern: majority olmal�d�r.  
   Yani, yap�lan okuma, �o�unlukla commit edilmi� t�m yazma i�lemlerini yans�t�r ve kesin tutarl� (strongly consistent) bir sonu� verir.  
   E�er yazma i�lemleri hala �o�unlu�a ula�mad�ysa, okuma sorgusu bu yazmalar�n �o�unlu�a ula�mas�n� bekleyebilir (yani sorgu, yazmalar �o�unlukta commit edilene kadar bekler).  
   Bu y�zden latency y�ksek olabilir.  
   Bu, tutarl�l�k i�in okuma i�leminin gerekti�inde bekleyebilece�i anlam�na gelir.  
   Sadece primary node �zerinde kullan�labilir.

5. **Snapshot**  
   Snapshot read concern�in garantileri sadece transaction commit i�lemi writeConcern "majority" ile yap�ld���nda ge�erlidir.  
   Race condition sorununu �nler.  
   Transactionlar aras� tutars�zl�klar engellenir.  
   Veri b�t�nl��� ve tutarl�l��� korunur.

---

## �rnekler

### �rnek 1:  Majority kullan�m�

Elimizde 3 d���ml� (node) bir MongoDB replica set var:  
- Node1: Primary  
- Node2: Secondary  
- Node3: Secondary  

**Yazma ��lemi**  
Uygulama bir belgeyi (�rne�in `{ userId: 1, name: "Ali" }`) Primary node�a yazar.  
Bu yazma i�lemi Node 1, 2 ve 3��n �o�unlu�u taraf�ndan onayland���nda (acknowledged), yani en az 2 d���mde i�lem tamamland���nda, veri **majority commit point**�e ula��r.  
Bu noktadan sonra veri art�k kal�c� ve �o�unluk taraf�ndan kabul edilmi� olur.

**Okuma ��lemi (readConcern: "majority")**  
MongoDB, bu durumda veriyi, en az �o�unlu�un onaylad��� ve commit etti�i en g�ncel haliyle d�ner.  
B�ylece, okunan veri en az iki d���mde var olan ve onaylanm�� veri olur.

---

### �rnek 2: Snapshot Read Concern ile ��lem Tutarl�l���

Diyelim hesap bakiyesinde 1000 TL var. Ayn� anda iki i�lem ger�ekle�iyor:  
- ��lem A: 200 TL �ekme  
- ��lem B: 300 TL �ekme  

- ��lem A, snapshot ile mevcut bakiye olarak 1000 TL�yi okur.  
- ��lem B, snapshot ile mevcut bakiye olarak 1000 TL�yi okur.  
- ��lem A, 200 TL �ekimini yapar, ancak transaction bitmedi�i i�in di�er i�lemler bundan etkilenmez (bakiye hala 1000 TL olarak g�r�n�r).  
- ��lem B, 300 TL �ekimini yapar, ancak transaction bitmedi�i i�in di�er i�lemler bundan etkilenmez.  
- ��lem A commit edilir, bakiye 800 TL olur.  
- ��lem B commit edilmeye �al���l�r ancak hata olu�ur ��nk� snapshot al�n�rken bakiye 1000 TL idi.  
- Bu y�zden ��lem B iptal edilir ve retry mekanizmas� ile tekrar denenir.

---

## Horizonal Sharding'da Veri Da��t�m�

Diyelim elimizde b�y�k bir kullan�c� koleksiyonu var ve shard key olarak `userId` kullan�yoruz.  
MongoDB, `userId` aral�klar�na g�re veriyi farkl� shard�lara (�rne�in Shard A, Shard B) yatay olarak b�ler.  
Her shard kendi alt k�mesindeki kullan�c� verilerini tutar.

---

## Orphaned Dok�man Riski Nas�l Olu�ur?

- Shard A�daki baz� kullan�c�lar art�k Shard B�ye ta��nacak (chunk migration).  
- MongoDB, bu veriyi Shard B�ye kopyalar.  
- Kopyalama bittikten sonra Shard A�dan bu veriler silinir.  
- Ancak bazen a� gecikmesi, kesinti ya da hata nedeniyle Shard A�daki eski veriler (art�k Shard B�ye ait olanlar) tam olarak temizlenmeyebilir.  
- ��te bu Shard A�daki fazla kalan dok�manlar **orphaned dok�man** olur.  
- E�er okuma sorgusu bu orphaned dok�manlar� da d�nd�r�rse, ayn� veri veya eski veri birden fazla shard�dan okunabilir, bu da tutars�zl��a yol a�ar.

---

## Sharding�de local ile available read concern seviyeleri aras�nda orphaned document fark� nas�l ortaya ��kar?

- Elimizde iki shard olsun:  
  - Shard A: userId 1-1000 aras� kullan�c�lar� tutuyor.  
  - Shard B: userId 1001-2000 aras� kullan�c�lar� tutuyor.  

- Chunk migration ile Shard A�daki 800-1000 aral���ndaki kullan�c�lar� Shard B�ye ta��mak istiyoruz.  
- Ta��ma tamamland�ktan sonra Shard A�daki bu kullan�c�lar silinir. Ancak, network sorunlar� veya di�er aksakl�klardan dolay� Shard A�da silinmesi gereken baz� veriler kalabilir (bunlar orphaned documents olur).  

**�rnek:**  
Sorgumuz `userId = 950` olan kullan�c�y� getirmek olsun.  

- E�er `readConcern: local` kullan�l�rsa, sorgu verinin g�ncel hali olan Shard B �zerinden yap�l�r. ��nk� MongoDB, shard key�e g�re do�ru shard�a y�nlendirme yapar ve en g�ncel veriyi okur.  
- Ancak `readConcern: available` kullan�l�rsa, okuma m�mk�n olan en h�zl� shard�dan yap�l�r. E�er sorgu Shard A�ya giderse, silinmesi gereken ve art�k ge�ersiz olan orphaned document okunabilir. Bu da tutars�z veri d�nme riskini art�r�r.

---

# Sonu�

MongoDB, replikasyon ve tutarl�l�k y�netimini, replica set yap�s� ile primary-secondary modeli �zerinden sa�lar. Read preference ve read/write concern ayarlar�yla, uygulaman�n ihtiya�lar�na g�re tutarl�l�k ve eri�ilebilirlik dengesi esnek�e y�netilir. Sharding ise yatayda veriyi da��tarak �l�eklenebilirlik sa�lar ancak do�ru okuma tutarl�l��� ayarlar� yap�lmazsa orphaned document gibi tutars�zl�klara sebep olabilir.

---

# Kaynaklar ve Detaylar

- MongoDB Resmi Dok�mantasyonu  
- CAP Teoremi a��klamalar�  
- Replica Set ve Sharding mimarileri  

---