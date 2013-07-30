Simple SceneGraph for Realtime Rendering Lecture
================================================

- Bewegung der Kamera mit W, A, S, D. �nderung der Sichtrichtung durch gedr�ckthalten von Mouse1 und Bewegung der Maus. Space bewegt die Kamera nach oben, Ctrl nach unten.
- Selektion des n�chsten / letzten Objekts mit dem Mausrad oder J, K
- Bewegung eines Objekts mit dem Numpad. Numpad 5 wechselt zwischen Rotations- und Translationsmodus. Transformation erfolgt dann mit Numpad 4, 6; 2, 8; 7, 9. Durch gedr�ckthalten von Shift wird alles beschleunigt.
- Toggle der Textur des ausgew�hlten Objekts mit T, Toggle von Wireframe mit I, Toggle von Tessellierung mit E.
- L�schen des ausgew�hlten Objekts mit Del / Entf.
- Kopieren des ausgew�hlten Objekts mit C.
- Parenting eines Objekts an ein anderes:
    - Auswahl eines Objektes und dr�cken von P
    - Auswahl eines anderen Objektes und dr�cken von P
    - Objekt 1 ist nun Kind von Objekt 2
- Animation eines Objektes:
    - Rotationen mit N
    - Translationen mit M

Zu den Aufgaben:

1.1. Loading Models
-----------------------------
- Es k�nnen nur .obj Modelle geladen werden. Es werden alle Modelle in dem "Models" Ordner rekursiv gesucht und geladen. Dabei werden Normalen, Tangenten und Binormalen berechnet und gegl�ttet. Die Berechnung der Gl�ttungen und der Normalen habe ich selbst geschrieben, wobei die Formeln zur Berechnung von Tangenten und Binormalen aus einem Tutorial f�r Normalmapping kamen.

1.2. Hierarchical Transformations
-----------------------------
- Modelle k�nnen interaktiv aneinander gebunden werden. Transformationen werden dabei propagiert. Es existieren auch Modelle welche selbst eine Hierarchie enthalten, wie z.B. "guy.obj" mit 2 Kindern, oder "mrbeep.obj" mit 81 Kindern. Durch Auswahl des Vaterknotens und Anwendung von Transformationen werden diese auch auf den Kindern durchgef�hrt. Dieser Teil wurde auch von mir selbst geschrieben.

1.3. Instancing
-----------------------------
- Das Kopiern von Modellen mit C kopiert nur Pointer auf die relevanten Buffer. Die Kopien bekommen jedoch neue Materialeigenschaften und Transformationsmatrizen. Dieser Teil wurde auch von mir selbst geschrieben.

1.4. Move Objects
-----------------------------
- Bewegung eines Objekts mit dem Numpad. Numpad 5 wechselt zwischen Rotations- und Translationsmodus. Transformation erfolgt dann mit Numpad 4, 6; 2, 8; 7, 9. Durch gedr�ckthalten von Shift wird alles beschleunigt. Dieser Teil wurde auch von mir selbst geschrieben.

1.5. Texturing & Lighting
-----------------------------
- Falls es f�r eine .obj Datei auch eine .mtl Datei mit gleichem Dateinamen gibt, wird diese verwendet um Materialeigenschaften und Texturen zu lesen. Sonst werden Defaultwerte,  bzw. einfach Texturen mit gleichem Dateinamen verwendet. Zu jeder Textur muss auch eine Normalmap existieren. Es wird sichergestellt dass eine Textur welche mehrmals verwendet wird nur einmal geladen wird. Togglen der Textur mit T, Togglen von Wireframe mit I. 
- Als Beleuchtungsmodell wurde Phong Lighting mit directional lights verwendet, wobei beliebig viele Lichtquellen existieren k�nnen. Die Lichtquellen k�nnen in RootNode.cs erg�nzt werden, wobei f�r jedes Licht eine Farbe und eine Richtung angegeben werden muss. 
- Dieser Teil wurde auch von mir selbst geschrieben.

1.6. Animation
-----------------------------
- Es existieren einfache Animationen, welche ein Objekt rotieren oder bewegen. Die Bewegung bewegt ein Objekt entlang eines Quadrats, die Rotation dreht ein Objekt entlang der Achsen nacheinander um einen bestimmten Winkel.
- Anwenden der Bewegung mit M, der Rotation mit N.
- Dieser Teil wurde auch von mir selbst geschrieben.

1.A - Advanced Texturing
-----------------------------
- Es wurde normal mapping implementiert. Die Formeln f�r die Berechnung von Tangenten und Binormalen wurden aus einem Tutorial entnommen. Das smoothing der Tangenten und Binormalen schreib ich selber.

2.1 - Simple Distance-Switched LOD
-----------------------------
- Falls in einem Ordner mehr als eine .obj Datei existiert werden diese als LOD-Stufen interpretiert, wobei das 1. Modell das am detailliertesten ist. Es wird ein LOD knoten erstellt, welcher immer nur das Kind rendert welches f�r die aktuelle Kameradistanz zust�ndig ist.
- Da nur 1 Modell LOD-Stufen hat, kann in Program.cs der Boolean LoadOnlyLodModels auf true gesetzt werden um nur LOD-Modelle zu laden damit der wechsel zwischen den LOD-Stufen leichter sichtbar ist.
- Dieser Teil wurde auch von mir selbst geschrieben.

2.2 - Hierarchical View-Frustum Culling
-----------------------------
- Zu jedem Knoten gibt es eine BoundingSphere. Diese wird durch die Transformationen geupdatet und mit seinen Kindern vereinigt. Vor dem rendern jedes Knotens wird �berpr�ft ob sich das ViewFrustrum der Kamera mit dem BoundingSphere schneidet. 
- Die Klassen BoundingSphere und ViewFrustrum waren schon Teil von "xnamath.h", sodass die Implementation hier einfach war.

2.D - Continuous LOD
-----------------------------
- Es wurde ein einfacher Tessellierungsalgorithmus implementiert, "Phong Tessellation". Je nach Distanz des zu tessellierenden Objekts wird st�rker oder weniger tesselliert. Man sieht den Effekt besonders an Sillhouetten, wobei Modelle mit wenig Polygonen verformt werden k�nnen.
- Toggle der Tessellierung mit E.
- Diesen Teil habe ich selber geschrieben, mit entnahme der Formeln aus dem Phong Tessellation paper.

