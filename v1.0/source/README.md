# Flicker — Windows

Uruchom `Flicker.exe`. Wybierz klawisz i tryb, ustaw czas, a następnie kliknij **Dodaj**. Możesz dodać wiele niezależnych wpisów, wyłączać je checkboxem, edytować i usuwać.

- **Co ile sekund**: od 0,05 do 86400 sekund. Pierwsze naciśnięcie następuje po podanym interwale, po zakończeniu odliczania startowego.
- **O godzinie**: codziennie o HH:mm:ss według lokalnej daty, godziny i strefy Windows.
- **Losowe opóźnienie do (ms)**: osobny limit dla każdego wpisu, od 0 do 60000 ms. Przed każdym wykonaniem losowana jest nowa wartość od 0 do limitu (włącznie). Np. interwał 1 s i limit 500 ms daje odstępy 1–1,5 s, z dokładnością timera Windows. Dla trybu godzinowego opóźnienie dodawane jest do wskazanej godziny. Wartość 0 wyłącza losowanie. Ustawienie jest zapisywane i dostępne w oknie edycji; stare wpisy domyślnie mają 0. Stop/F9 anuluje oczekujące naciśnięcie. Losowanie nie ukrywa użycia SendInput i nie gwarantuje niewykrywalności.
- **F8** lub przycisk Start: włączanie i wyłączanie. Po starcie masz 3 sekundy na przełączenie okna.
- **Przytrzymanie od/do (ms)**: losowany niezależnie dla każdego naciśnięcia czas pomiędzy wciśnięciem a puszczeniem klawisza. Zakres ustawień to 1–5000 ms; domyślnie 50–100 ms, także dla starszych zapisanych wpisów. To konfigurowalny przykład, nie statystyczny model ludzkiego pisania ani grania. Równe wartości dają stały czas. Stop/F9 i zamknięcie programu zwalniają przytrzymywane klawisze. Nieudane zwolnienia są ponawiane i blokują kolejny start. Wpis próbujący nacisnąć już przytrzymany przez clicker klawisz zostaje pominięty. Różne klawisze mogą być przytrzymywane równocześnie. Czas jest przybliżony i zależy od timera oraz obciążenia Windows.
- **F9**: globalne zatrzymanie, także gdy okno clickera jest zminimalizowane. F8/F9 są zarezerwowane i nie występują na liście wysyłanych klawiszy. Jeśli F9 jest zajęty przez inny program, start jest blokowany.
- Naciśnięcia trafiają do okna znajdującego się na pierwszym planie. Gdy aktywny jest sam clicker, zdarzenia są pomijane. Harmonogram nie nadrabia pominiętych naciśnięć.
- D0–D9 oznaczają cyfry w górnym rzędzie klawiatury; Space to spacja, Back to Backspace.

Lista zapisuje się automatycznie w `%LOCALAPPDATA%\KeyboardClicker\rules.xml`. Po ponownym uruchomieniu program zawsze pozostaje zatrzymany. Liczniki resetują się po starcie.

Program używa Windows **SendInput** z kodami skanowania oraz parą naciśnięcie/puszczenie. Nie uruchamia poleceń ani procesów w celu wykonywania wpisów. Nie jest to fizyczna klawiatura USB: gry lub aplikacje filtrujące wejście syntetyczne mogą je odrzucić. Klawisze trzymane fizycznie (np. Ctrl, Shift) mogą zmienić wynik. Windows ogranicza wysyłanie wejścia do aplikacji o wyższych uprawnieniach. Dokumentacja: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput

Aplikacja musi być uruchomiona, a komputer wybudzony z odblokowaną sesją. Dokładność zależy od obciążenia Windows (timer sprawdza zadania co około 10 ms). Zadanie godzinowe ma okno wykonania poniżej 2 sekund; starsze terminy są pomijane. Po cofnięciu zegara nie jest wykonywane drugi raz tego samego dnia w tej samej sesji programu. Zmiana czasu letniego może pominąć nieistniejącą godzinę.

## Budowanie

Uruchom `powershell -ExecutionPolicy Bypass -File .\build.ps1` w katalogu projektu. Używany jest kompilator .NET Framework dostarczony z Windows, bez pakietów zewnętrznych. Kod źródłowy: `Clicker.cs` oraz `Theme.cs` (wygląd interfejsu).

Testy i podglądy bez wysyłania klawiszy: uruchom verify.ps1.

Interfejs: domyślna szerokość okna 630 px, minimalna 630 px. Przyciski są białe, prostokątne i korzystają ze standardowego renderowania Windows Forms. Timer harmonogramu pracuje co około 10 ms podczas działania, a w spoczynku co 250 ms. Tabela odświeża tylko zmienione wartości, najwyżej 10 razy na sekundę. Przebudowa listy nie wywołuje zapisów ustawień.

Flicker ma ikonę F osadzoną w pliku EXE i w oknach aplikacji. Build tworzy wyłącznie Flicker.exe. Ustawienia pozostają w dotychczasowym katalogu KeyboardClicker. Źródło ikony: assets/icon-f-proposal.png; wielorozmiarowa ikona Windows: assets/flicker.ico.
## Kliknięcie myszy w punkcie ekranu

1. W menu „Mysz ▾” obok pola klawisza wybierz **Mysz: lewy**, **Mysz: prawy** lub **Mysz: środkowy**.
2. Kliknij **Punkt myszy…**, potem **Wybierz punkt za 3 s**. Przenieś kursor w miejsce docelowe i zaczekaj — nie klikaj. Zatwierdź **Zapisz punkt**. Współrzędne X/Y można również wpisać ręcznie; ujemne współrzędne obsługują monitory po lewej lub nad ekranem głównym.
3. Ustaw interwał albo godzinę, losowe opóźnienie i przytrzymanie, następnie **Dodaj do listy**. Każdy wpis zapamiętuje własny punkt. W „Edytuj wybrany” można zmienić przycisk i punkt.

Flicker przesuwa prawdziwy kursor systemowy i wysyła naciśnięcie oraz puszczenie przycisku przez SendInput. Po kliknięciu kursor pozostaje w punkcie docelowym. Punkt jest stałą pozycją ekranu: zmiana położenia okna, rozdzielczości lub układu monitorów wymaga ponownego wskazania miejsca. Program nie rozpoznaje elementu pod kursorem. Nie poruszaj myszą w trakcie przytrzymania, aby nie przeciągać elementów.

Przerwa, F9 i Stop zwalniają też przyciski myszy. Inne kliknięcie myszy zaplanowane podczas przytrzymania jednego przycisku zostaje pominięte, aby Flicker nie przeciągał kursora między punktami. Klawiatura nadal ma niezależne harmonogramy. Jeżeli Flicker jest aktywnym oknem, wszystkie akcje pozostają pomijane jak wcześniej; jeśli jego okno zasłoni punkt podczas pracy w tle, następuje zatrzymanie z komunikatem. Punkt poza podłączonymi monitorami także zatrzymuje działanie.

Dokumentacja wejścia myszy Windows: https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-mouseinput
## Wybór klawisza przez naciśnięcie

Kliknij pole z nazwą klawisza (domyślnie Space), a potem naciśnij klawisz. Enter, Tab, Escape i strzałki są przechwytywane jako wybór, bez zatwierdzania okna i zmiany fokusu. Obsługiwane są pojedyncze klawisze, nie kombinacje Ctrl/Alt/Shift. F8 i F9 pozostają zarezerwowane. Niedostępny klawisz zachowuje poprzedni wybór. Przyciski myszy są pod menu Mysz ▾. Tak samo działa edycja wpisu.

## Wstrzymanie jako wpis listy

Kliknij **+ Wstrzymaj…** i wybierz początek oraz **Na X minut** albo **Do godziny**. Od 09:59:59 na 10 minut oznacza koniec o 10:09:59; do godziny 10:10:00 oznacza koniec dokładnie wtedy, z dokładnością timera Windows.

Każde wstrzymanie jest osobnym codziennym wpisem, który można wyłączyć, edytować i usunąć. Przedziały mogą przechodzić przez północ. Wcześniejsza godzina końca oznacza kolejny dzień; równe godziny są niedozwolone. Zakres czasu trwania to 0,01–1440 minut. Nakładające się wstrzymania łączą się: aplikacja czeka, aż żadne nie będzie aktywne.

Na początku Flicker zwalnia swoje przytrzymane klawisze i przyciski myszy oraz anuluje oczekujące opóźnienia. Wstrzymanie ma pierwszeństwo przed zwykłymi wpisami. Po końcu interwały zaczynają się od nowa, bez nadrabiania pominiętych kliknięć i **bez automatycznego Enter**. Start w trakcie przedziału także wstrzymuje wykonywanie. F9/Stop anuluje działanie; zakończenie przedziału nie uruchomi programu po ręcznym Stop. Zmiana zegara Windows wpływa na przedziały.

Poprzednia aktywna przerwa z pause.xml jest jednorazowo przenoszona jako wpis Wstrzymaj do godziny, bez dawnego klawisza wznowienia. Oryginalny plik pozostaje kopią, a pause.xml.migrated zapobiega ponownemu importowi.

Aktualne testy: `powershell -ExecutionPolicy Bypass -File .\verify.ps1`. Skrypt kompiluje wszystkie aktualne źródła, sprawdza logikę bez wysyłania klawiszy i tworzy podglądy w ignorowanym katalogu obj. Nowe źródła: KeyPicker.cs, ScheduledPause.cs, ScheduleTests.cs.