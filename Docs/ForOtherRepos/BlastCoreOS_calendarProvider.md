# Zadanie dla agenta BlastCoreOS: dostawy z modów w kalendarzu

> 2026-11-25, od agenta OXL. Do wklejenia agentowi BlastCoreOS (repo `C:\Users\usmat\source\repos\BlastCoreOS`).

```
OXL (C:\Users\usmat\source\repos\CMS2026_OXL) sprzedaje części, silniki, narzędzia i dekoracje z dostawą "jutro albo pojutrze". Paczka w systemie gry (DeliveryManager) ma tylko godzinę, bez dnia, więc OXL trzyma ją u siebie i przekazuje grze dopiero o północy dnia dostawy. Kalendarz BlastCoreOS (10 Calendar, BlastCoreCalendarData.GetDeliveries) czyta tylko DeliveryManager, więc zakup z OXL pojawia się w kalendarzu dopiero w dniu dostawy. Gracz zgłosił: "item, który zamówiłem, nie pojawia się w kalendarzu". calendar.md pkt 8 przewiduje "wpisy z modów przez API".

1. Publiczne API (nazwa wg konwencji, np. BlastCoreCalendarAPI w 10 Calendar), kontrakt jak BlastCoreBankAPI (zasada 8, wpis w Docs/Design/modules.md). Nie zmieniaj istniejącego API.
2. Rejestracja dostawcy paczek: RegisterDeliveryProvider(string modId, Func<DateTime, IEnumerable<BlastCoreCalendarExternalDelivery>> provider). Argument = "teraz" kalendarza (BlastCoreGameTime), mod zwraca swoje paczki w drodze. Ten sam modId drugi raz zastępuje dostawcę. Wywołania na głównym wątku, wyjątek dostawcy łapany i logowany (wzór: BlastCoreWebAPI.RegisterSearchProvider).
3. BlastCoreCalendarExternalDelivery: tylko pola proste: string Id, DateTime At (dzień i godzina dostawy), string Sender (np. "OXL Marketplace"), string Title, string[] Items, bool Arrived (false = w drodze).
4. Kalendarz pokazuje te paczki razem z paczkami gry: w "On the way", w siatce miesiąca (kropka w dniu) i w planie dnia o godzinie At, z nazwą nadawcy zamiast sklepu gry. Signature uwzględnia paczki modów, żeby widok się odświeżał.
5. Paczka z modu znika z listy, gdy mod przestanie ją zwracać (OXL przekazuje ją wtedy grze i dalej pokazuje ją kalendarz z DeliveryManager jako paczkę "Used parts" / "Decorations and tools").
6. Goal na sprawdzenie i wpis w modules.md (Historia API). Daj znać nazwę klasy i metody; OXL podepnie dostawcę (ma już ItemListingSystem.GetPendingParcels: nazwa, rodzaj, sekundy do dostawy).
```
