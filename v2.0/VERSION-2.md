# Flicker 2.0 — kierunek zmian

Wersja 1.0: tag Git `v1.0`; lokalne archiwum EXE i kodu w `../v1.0`.
Prace 2.0: gałąź `version-2`.

Priorytety wybrane przez użytkownika:
- Ciemny interfejs z turkusowym akcentem.
- Kompaktowe okno podczas działania.

Przegląd 1.0: formularz stale zajmował większość ekranu, lista miała zbyt mało miejsca, stan pracy był schowany na dole, a część informacji o akcji była zlepiona w jednej kolumnie. Przyciski wymagają standardowego renderowania Windows Forms, bez ponownego wprowadzania problematycznych zaokrągleń.

Pierwszy zakres 2.0: lista jako główny widok, dodawanie klawiatury/myszy/wstrzymania w dialogach, czytelny stan pracy, liczniki, przełączanie widoku kompaktowego, opcjonalny kompakt po starcie, przypięcie nad innymi oknami, dwuklik do edycji i duplikowanie akcji. Zachowane dotychczasowe harmonogramy oraz F8/F9.

Ustawienia wersji 2.0 używają osobnego katalogu FlickerV2 w LocalAppData. Przy pierwszym uruchomieniu lista jest odczytywana z wersji 1.0 i zapisywana jako oddzielna kopia. Testy używają danych przykładowych bez zapisu ustawień i wysyłania wejścia.
