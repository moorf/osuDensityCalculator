# osuDensityCalculator

Inefficient osu!std hitobject density calculator class library for detecting stream maps.
Can be used in CarouselBeatmap for filtering and OsuBeatmap for displaying the value. 

My own algorithm for detecting stream maps.
Takes some time to generate values on first launch (first time filtering in CarouselBeatmap), then takes values from cache and works like a charm.
