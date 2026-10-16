# How a hoe is really used (reference for note 155)

Chris, 2026-10-12 09:47: "looks like they are just banging into onto the ground. research and look up videos online to see how people actually use hoes".

## Sources
1. Eliot Coleman, *The New Organic Grower*, excerpt in the Collinear Hoe tech sheet: https://cms.activevista.com.au/wp-content/uploads/2020/12/JSS-collinear-hoe-tech-sheet.pdf
2. Easy Digging, "How To Use a Grape Hoe for weeding and moving soil": https://www.easydigging.com/gardening/articles-g/garden-hoes/how-to-use-grape-hoe.html
3. Hartford Courant, "How to dig up weeds with a garden hoe" (2021): https://www.courant.com/2021/04/20/how-to-dig-up-weeds-with-a-garden-hoe/
4. Guided Review, "How To Use A Hoe Garden Tool": https://guidedreview.com/how-to-use-a-hoe-garden-tool/
5. Urban Homesteading PDX, "How to Use a Garden Hoe": https://www.urbanhomesteadingpdx.com/how-to-use-a-garden-hoe/
6. Luttrell Psalter (c. 1325-35), weeding margin: a forked stick and a hooked blade on a stick (British Library / Science Photo Library C016/7678). Medieval weeding was a pull with a hook too, not a bang.

## The motion
- **Tool:** a draw (paddle) hoe: a flat blade at about 90 degrees to the handle, pulled toward you [3][4][5]. (Our farmers had a pitchfork, which is the wrong tool for this. They now carry a draw hoe.)
- **Grip:** both hands on the handle, spread about shoulder width, one near the end and one nearer the blade, held like a broom with thumbs up the handle. The hands move together to draw, and adjust separately to set the blade's angle [1][4][5].
- **Stance:** mostly upright, knees slightly bent, one foot a little ahead of the other, rocking the weight back and forth with the stroke. Pull with the body's weight rather than the arms alone; the shoulders drive and the hands guide [1][2]. A bent back belongs to the chopping hoe, not to weeding [1].
- **Stroke:** the hands go forward and a little up as the hoe is raised (a modest lift, never overhead). The blade is set into the soil a little past the target, at a shallow angle just under the surface, then pulled back toward the feet in a low arc. The head traces an oval: pull back, raise, reach forward, lower, pull back [2]. A short, light downward chop is for clods and tough roots only [3][5].
- **Soil:** the cut soil and weeds pile up in the direction of the pull, near the feet [2].
- **Rhythm:** short, controlled, overlapping strokes [3][4], about one a second or two.
- **Along the row:** keep moving away from the worked ground; the strokes overlap [2][4].

## What the game does now (ActorVisual.Field.cs)
Reach forward and lift (the blade about 35 cm up, about 1.15 m ahead), then set in at an angle (a short downward entry), then draw back about 55 cm toward the feet with the blade just under the surface, pushing a clod. Then recover and step. Back bend 8-14 degrees, hands about 42 cm apart, one stroke every 1.8 s. Test: `FieldWorkTests.A_farmer_at_work_in_a_field_hoes_with_a_two_handed_stroke` requires the blade to travel at least 30 cm toward the body while it is in the soil.