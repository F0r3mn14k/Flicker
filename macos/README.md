# Flicker dla macOS

Projekt korzysta z .NET 8 oraz Avalonia. Ten sam kod interfejsu i silnik harmonogramu budują się dla Maców Intel (`osx-x64`) oraz Apple Silicon (`osx-arm64`).

## Uprawnienia

Przed uruchomieniem automatyzacji otwórz **Ustawienia systemowe → Prywatność i ochrona → Dostępność** i dodaj Flicker. Bez tego macOS nie pozwoli aplikacji wysyłać zdarzeń klawiatury ani myszy.

## Uruchomienie deweloperskie

Na Macu z .NET SDK 8:

```sh
cd Flicker/macos/Flicker.Mac
dotnet run
```

Projekt obsługuje profile, eksport/import XML, interwały, godziny, zaplanowane oraz ręczne pauzy i wpis master. Wpis myszy przyjmuje współrzędne X/Y ekranu. Lista klawiszy zawiera także `-`, `=`, przecinek, kropkę, ukośnik, nawiasy, średnik, apostrof, backslash i `` ` ``. Kod klawiatury opiera się na fizycznym układzie klawiszy macOS, więc znak w aplikacji docelowej zależy od układu klawiatury ustawionego w macOS.

## Pakowanie

Na Macu uruchom:

```sh
cd Flicker/macos
chmod +x publish-macos.sh
./publish-macos.sh
```

Skrypt tworzy osobne wydania dla `osx-x64` i `osx-arm64`. Finalny pakiet `.app` wymaga jeszcze przygotowania na Macu, podpisu Apple oraz notaryzacji. Wymaga to konta Apple Developer i narzędzi Xcode; nie da się wiarygodnie wykonać tych kroków z komputera Windows.
