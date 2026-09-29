# Flicker 4.0

## Nowości

- Nazwane profile: tworzenie, kopiowanie, zmiana nazwy i usuwanie.
- Eksport i import profilu w pliku .flicker.xml. Profil obejmuje harmonogram i sekwencję.
- Edytor sekwencji: klawisze, kliknięcia myszy, oczekiwanie, powtórzenia i wykonanie pojedynczego kroku.
- Ciemny interfejs, widok kompaktowy i suwaki przy ustawieniach liczbowych.
- Jedna wartość przytrzymania dla nowych i edytowanych akcji; domyślnie 80 ms.
- Poprawione rozpoznawanie klawiszy interpunkcyjnych, w tym -, =, przecinka i nawiasów.

## Pobieranie i uruchomienie

Pobierz Flicker-4.0.exe albo rozpakuj Flicker-4.0-Windows.zip i uruchom zawarty plik EXE. Aplikacja jest przeznaczona dla Windows i korzysta z .NET Framework. Nie wymaga instalatora.

F8 uruchamia/zatrzymuje, F9 zatrzymuje awaryjnie. Po starcie są 3 sekundy na przełączenie do okna docelowego. Wejście jest wysyłane przez Windows SendInput do aktywnej aplikacji.

## Dane i zgodność

Ustawienia 4.0 są zapisywane w LocalAppData/FlickerV4. Przy pierwszym uruchomieniu importowane są wcześniejsze ustawienia, a oryginały pozostają bez zmian. Eksport profilu nie zawiera liczników ani stanu uruchomienia. Po przeniesieniu profilu na inny komputer należy sprawdzić współrzędne myszy.

Kod starszych wersji znajduje się w osobnych folderach repozytorium. Wersja 4.0 nie zawiera jeszcze nagrywania sekwencji ani kombinacji klawiszy.

## Weryfikacja

Kompilacja oraz testy harmonogramów, klawiatury, myszy, sekwencji, profili, importu/eksportu i suwaków zakończone poprawnie. Podglądy interfejsu sprawdzone. Testy automatyczne nie wysyłają klawiszy do innych aplikacji.
