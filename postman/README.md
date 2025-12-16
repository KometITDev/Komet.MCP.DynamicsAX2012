# BCProxy API - Postman Tests

Diese Postman Collection enthält alle verfügbaren BCProxy API Endpunkte mit Beispiel-Requests.

## Import in Postman

1. **Postman öffnen**
2. **Collection importieren:**
   - `File` → `Import`
   - `BCProxy API.postman_collection.json` auswählen
3. **Environment importieren:**
   - `File` → `Import`  
   - `BCProxy-Local.postman_environment.json` auswählen
4. **Environment aktivieren:**
   - Oben rechts: `BCProxy - Local` auswählen

## Voraussetzungen

Der BCProxy muss laufen auf: `http://localhost:5100`

```bash
# BCProxy starten (im Projektverzeichnis)
cd src\Komet.MCP.DynamicsAX2012.BCProxy
dotnet run
```

## API Endpunkte

### Health
- `GET /api/health` - Health Check

### Customer
- `GET /api/customer/{accountNum}?company=GBL` - Einzelnen Kunden abrufen (Basisdaten)
- `GET /api/customer/{accountNum}?company=GBL&includeAddresses=true` - Kunden mit Adressen
- `GET /api/customer/{accountNum}?company=GBL&includeContacts=true` - Kunden mit elektronischen Adressen (Telefon, E-Mail)
- `GET /api/customer/{accountNum}?company=GBL&includeAddresses=true&includeContacts=true` - Kunden komplett
- `GET /api/customer/search?accountNum=234*&company=GBL` - Kunden suchen (Wildcard)
- `GET /api/customer/search?customerGroup=D-INL&company=GBL` - Kunden nach Gruppe suchen
- `GET /api/customer/search/address?zipCode=32*&company=GBL` - Kunden nach PLZ suchen
- `GET /api/customer/search/address?city=Lemgo&company=GBL` - Kunden nach Ort suchen
- `GET /api/customer/search/address?zipCode=32657&city=Lemgo&company=GBL` - Kunden nach PLZ + Ort

### Product
- `GET /api/product/{itemId}?company=GBL` - Einzelnes Produkt abrufen (Basisdaten)
- `GET /api/product/{itemId}?company=GBL&language=de` - Produkt mit deutscher Bezeichnung
- `GET /api/product/{itemId}?company=GBL&language=en` - Produkt mit englischer Bezeichnung
- `GET /api/product/{itemId}?company=GBL&includeCategories=true` - Produkt mit Kategorien

### Sales Order
- `GET /api/salesorder?salesId=VKA/002326961&company=GBL` - Einzelnen Auftrag abrufen (inkl. Lines)
- `GET /api/salesorder/search?customerAccount=234760&company=GBL` - Aufträge nach Kunde (ohne Lines)
- `GET /api/salesorder/search?customerAccount=234760&company=GBL&includeLines=true` - Aufträge mit Lines
- `GET /api/salesorder/search?salesId=VKA*&company=GBL` - Aufträge suchen (Wildcard)

### X++ Execute
- `POST /api/ax/execute` - X++ Methode ausführen

## Environment Variablen

Die Environment-Datei definiert:

| Variable | Wert | Beschreibung |
|----------|------|--------------|
| `baseUrl` | `http://localhost:5100` | BCProxy URL |
| `company` | `GBL` | Standard-Firma |

Diese können in Postman angepasst werden für verschiedene Umgebungen.

## Beispiel-Response

### Customer (Basisdaten)
```json
{
  "accountNum": "234760",
  "name": "Musterfirma GmbH",
  "customerGroup": "D-INL",
  "currency": "EUR",
  "company": "GBL",
  "primaryAddress": {
    "street": "Musterstraße 1",
    "city": "Lemgo",
    "zipCode": "32657",
    "countryRegionId": "DE"
  }
}
```

### Customer mit Adressen und Kontakten
```json
{
  "accountNum": "234760",
  "name": "Musterfirma GmbH",
  "customerGroup": "D-INL",
  "currency": "EUR",
  "company": "GBL",
  "addresses": [
    {
      "street": "Musterstraße 1",
      "city": "Lemgo",
      "zipCode": "32657",
      "state": "",
      "countryRegionId": "DE",
      "fullAddress": "Musterstraße 1, 32657 Lemgo",
      "isPrimary": true,
      "isPrivate": false,
      "addressType": "Business, Delivery, Invoice"
    }
  ],
  "contacts": [
    {
      "type": "Phone",
      "value": "+49123456789",
      "isPrimary": true,
      "description": "Hauptnummer"
    },
    {
      "type": "Email",
      "value": "info@musterfirma.de",
      "isPrimary": false,
      "description": ""
    }
  ]
}
```

### Address-Felder Erklärung
- **isPrimary**: Hauptadresse des Kunden (aus DirPartyLocation)
- **isPrivate**: Private Adresse (aus DirPartyLocation)
- **addressType**: Kombinierte Rollen aus DirPartyLocation:
  - `Business` - Geschäftsadresse
  - `Delivery` - Lieferadresse
  - `Home` - Privatadresse
  - `Invoice` - Rechnungsadresse
  - Mehrere Rollen werden kombiniert: "Business, Delivery, Invoice"

### Product (Basisdaten)
```json
{
  "itemId": "10001",
  "itemName": "Diamantbohrer",
  "description": "Bohrer für Kariesentfernung und Präparation",
  "nameAlias": "DB-001",
  "company": "GBL",
  "itemGroupId": "INST",
  "itemType": "Item",
  "primaryUnitId": "Stk",
  "inventUnitId": "Stk",
  "purchUnitId": "Stk",
  "salesUnitId": "Stk",
  "netWeight": 0.05,
  "grossWeight": 0.06,
  "stopped": false,
  "standardConfigId": "",
  "brasItemIdBulk": "",
  "brasOptNumofRevolutions": 40000,
  "brasMaxNumofRevolutions": 50000,
  "brasProductTypeId": "BOHRER",
  "brasFigure": "Kugel",
  "brasShank": "FG",
  "brasSize": "012"
}
```

### Product mit Kategorien
```json
{
  "itemId": "10001",
  "itemName": "Diamantbohrer",
  "description": "Bohrer für Kariesentfernung und Präparation",
  "categories": [
    {
      "categoryId": 5637144576,
      "categoryName": "Chirurgie",
      "categoryHierarchyName": "Medizinprodukte"
    },
    {
      "categoryId": 5637144577,
      "categoryName": "Diamantinstrumente",
      "categoryHierarchyName": "Produkttypen"
    }
  ]
}
```

### Product-Felder Erklärung
- **language**: Sprachcode (de, en, fr, etc.) für itemName und description
- **includeCategories**: Produktkategorien aus EcoResCategory laden
- **Bras-Felder**: Custom-Felder für zahnmedizinische Instrumente
  - BrasOptNumofRevolutions / BrasMaxNumofRevolutions: Drehzahlangaben
  - BrasFigure: Instrumentenform (Kugel, Birne, Zylinder, etc.)
  - BrasShank: Schafttyp (FG, HP, RA, etc.)
  - BrasSize: ISO-Größe des Instruments

## Wildcard-Suche

Die API unterstützt `*` als Wildcard:
- `234*` - Alle AccountNums die mit "234" beginnen
- `Ber*` - Alle Städte die mit "Ber" beginnen (Berlin, etc.)

## Fehlerbehandlung

Bei Fehlern gibt die API HTTP 500 zurück mit Details:
```json
{
  "message": "Error searching customers: ..."
}
```
