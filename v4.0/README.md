# Flicker 4.0 — profile i sekwencje

Uruchom `Flicker-4.0.exe`. Główna lista nadal obsługuje niezależne akcje godzinowe, interwały i zaplanowane wstrzymania. Przycisk **Sekwencje →** otwiera nowy edytor. Harmonogram i sekwencja działają oddzielnie: zatrzymaj harmonogram przed otwarciem edytora.

## Pierwsza sekwencja

1. Dodaj **Klawisz**, np. 1, i ustaw czas przytrzymania.
2. Dodaj **Czekaj**, np. 2 sekundy.
3. Dodaj **Mysz** i wskaż punkt ekranu.
4. Dodaj **Klawisz**, np. Enter.
5. Ustaw liczbę powtórzeń, np. 20, i kliknij **Start** lub F8.
6. Masz 3 sekundy na przejście do okna docelowego. **F9** natychmiast zatrzymuje wykonywanie i zwalnia wejście.

Kroki zmieniasz dwuklikiem. Kolejność zmieniasz przeciąganiem wierszy lub przyciskami W górę/W dół. Dostępne są duplikowanie i usuwanie. **Jeden krok** wykonuje zaznaczony krok po 3 sekundach i kończy działanie, niezależnie od liczby powtórzeń.

Podczas pracy podświetlony jest bieżący krok. Na dole są numer powtórzenia, numer kroku, etap oraz pozostały czas. Edycja podczas działania jest zablokowana. Po ostatnim powtórzeniu sekwencja zatrzymuje się automatycznie. Ponowny Start rozpoczyna ją od początku.

## Zasady wykonania

Kolejny krok zaczyna się po zakończeniu poprzedniego, w tym zwolnieniu klawisza/przycisku. Czekaj oznacza osobny czas oczekiwania; nie działa jako harmonogram godzinowy. Opóźniony timer nie powoduje nadrabiania serii naciśnięć. Czas jest przybliżony (timer 25 ms), zależny od obciążenia Windows.

Gdy aktywne jest okno Flickera, krok wejścia czeka zamiast zostać pominięty. Kroki Czekaj nadal odmierzają czas. Akcje trafiają do aktualnie aktywnej aplikacji. Ruch myszy zmienia rzeczywistą pozycję kursora. Punkt zasłonięty Flickerem lub poza ekranami powoduje zatrzymanie z komunikatem.

Edytor obsługuje jeden scenariusz w każdym profilu, od 1 do 10000 powtórzeń, pojedyncze klawisze i kliknięcia oraz oczekiwanie 0,01–86400 sekund. Nie zawiera jeszcze nagrywania, kombinacji klawiszy, zagnieżdżonych pętli ani warunków opartych na zawartości ekranu.

## Profile, eksport i import

Kliknij **Profile: nazwa** w głównym oknie. **Nowy** tworzy pusty profil, **Kopia** kopiuje zaznaczony, **Nazwa** zmienia nazwę, a **Usuń** usuwa profil z biblioteki (ostatni profil musi pozostać). Każdy zawiera niezależną listę akcji, wstrzymania oraz sekwencję z liczbą powtórzeń.

Wybierz profil i kliknij **Zastosuj profil**, aby zapisać zmiany w oknie i aktywować zaznaczony profil. **Anuluj** lub zamknięcie tego okna odrzuca zmiany w bibliotece. Zmiany akcji i sekwencji w aktywnym profilu zapisują się automatycznie. Przełączanie profili jest dostępne po zatrzymaniu działania.

**Eksportuj** zapisuje zaznaczony profil do pliku `*.flicker.xml`. Wybierz lokalizację w oknie zapisu. Plik zawiera nazwę, akcje, punkty myszy, harmonogramy i sekwencję; nie zawiera liczników ani stanu uruchomienia. Eksport jest wykonywany od razu, niezależnie od późniejszego anulowania okna profili.

**Importuj** odczytuje taki plik jako nowy profil. Jeśli nazwa istnieje, dopisywany jest numer, np. `Praca (2)`. Po imporcie kliknij **Zastosuj profil**, aby zachować nowy profil. Import nie nadpisuje istniejącego i nie uruchamia akcji. Pliki starszych formatów `rules.xml` nie są plikami profilu — ich automatyczne przeniesienie opisano niżej. Stałe współrzędne myszy po przeniesieniu na inny komputer mogą wymagać poprawienia.

Biblioteka: `%LOCALAPPDATA%\FlickerV4\profiles.xml`; poprzedni zapis: `profiles.xml.bak`. Zapis zastępuje cały plik dopiero po przygotowaniu nowego. Błąd odczytu blokuje nadpisywanie biblioteki. Limit pliku to 16 MB, biblioteki 1000 profili, a pojedynczego profilu 10000 akcji i 10000 kroków.

Przy pierwszym uruchomieniu 4.0 powstaje profil Domyślny. Lista pochodzi z pierwszego dostępnego katalogu: FlickerV3, FlickerV25, FlickerV2, KeyboardClicker. Sekwencja pochodzi z FlickerV3, jeśli jest zapisana. Wersja 4.0 zapisuje zmiany osobno, bez modyfikowania starszych wersji. Kolejne uruchomienia przywracają ostatnio wybrany profil i pozostają zatrzymane.

## Budowanie i weryfikacja

`powershell -ExecutionPolicy Bypass -File .\build.ps1`

`powershell -ExecutionPolicy Bypass -File .\verify.ps1`

Nowe źródła: Sequence.cs (model i silnik), SequenceUI.cs (edytor), SequenceTests.cs (testy). Testy korzystają ze sztucznego czasu i odbiornika wejścia; nie wysyłają klawiszy do innych aplikacji. Obejmują kolejność, oczekiwanie, powtórzenia, zatrzymanie, pojedynczy krok, zwalnianie wejścia, zapis XML i błędy. Podglądy interfejsu są w obj.

Profile: Profiles.cs (format i zapis), ProfileUI.cs (zarządzanie), ProfileTests.cs (testy importu, eksportu, niezależności i kopii zapasowych).

## Suwaki wartości

Pola interwału, opóźnienia, przytrzymania, minut wstrzymania, czasu kroku oraz liczby powtórzeń mają zsynchronizowane suwaki. Nadal można wpisywać dokładne liczby — przesunięcie wskaźnika nie zaokrągla wartości wpisanej ręcznie. Duże zakresy używają skali nieliniowej, dokładniejszej dla małych wartości. Strzałki zmieniają wartość o krok pola, Home/End wybierają granice zakresu. Godziny oraz współrzędne pozostają w polach precyzyjnych.

Przytrzymanie to teraz jedna stała wartość (1–5000 ms), domyślnie 80 ms dla nowych akcji. Starsze profile zachowują zakres do momentu zapisania edycji wpisu; edytor pokazuje wcześniejszy zakres i proponuje jego górną wartość jako stały czas. Anulowanie nie zmienia wpisu. Format importu i eksportu pozostaje zgodny.
