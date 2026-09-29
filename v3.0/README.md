# Flicker 3.0 — sekwencje

Uruchom `Flicker-3.0.exe`. Główna lista nadal obsługuje niezależne akcje godzinowe, interwały i zaplanowane wstrzymania. Przycisk **Sekwencje →** otwiera nowy edytor. Harmonogram i sekwencja działają oddzielnie: zatrzymaj harmonogram przed otwarciem edytora.

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

Pierwsza wersja edytora obsługuje jeden zapisany scenariusz, od 1 do 10000 powtórzeń, pojedyncze klawisze i kliknięcia oraz oczekiwanie 0,01–86400 sekund. Nie zawiera jeszcze nagrywania, kombinacji klawiszy, zagnieżdżonych pętli ani warunków opartych na zawartości ekranu.

## Zapis i starsze wersje

Własne ustawienia: `%LOCALAPPDATA%\FlickerV3\rules.xml` i `sequence.xml`. Sekwencja zapisuje się po zmianach; poprzedni zapis zachowuje się jako `sequence.xml.bak`. Błąd odczytu blokuje nadpisanie uszkodzonego pliku. Lista klasyczna przy pierwszym uruchomieniu importuje ustawienia z pierwszego dostępnego katalogu: FlickerV25, FlickerV2, KeyboardClicker. Oryginały pozostają bez zmian.

## Budowanie i weryfikacja

`powershell -ExecutionPolicy Bypass -File .\build.ps1`

`powershell -ExecutionPolicy Bypass -File .\verify.ps1`

Nowe źródła: Sequence.cs (model i silnik), SequenceUI.cs (edytor), SequenceTests.cs (testy). Testy korzystają ze sztucznego czasu i odbiornika wejścia; nie wysyłają klawiszy do innych aplikacji. Obejmują kolejność, oczekiwanie, powtórzenia, zatrzymanie, pojedynczy krok, zwalnianie wejścia, zapis XML i błędy. Podglądy interfejsu są w obj.
