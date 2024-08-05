using System;
using System.Linq;
using NumFlat;
using NumFlat.IO;
using NumFlat.MultivariateAnalyses;
using NumFlat.SignalProcessing;
using ScottPlot;

static class Program
{
    static void Main(string[] args)
    {
        var frameLength = 1024;
        var frameShift = frameLength / 2;
        var window = WindowFunctions.SquareRootHann(frameLength);

        var (source, sampleRate) = WaveFile.ReadMono("piano.wav");
        var (spectrogram, info) = source.Stft(window, frameShift);
        var xs = spectrogram.Select(spectrum => spectrum.Map(x => x.MagnitudeSquared())).ToArray();

        var data = new double[xs[0].Count, xs.Length];
        for (var row = 0; row < xs[0].Count; row++)
        {
            for (var col = 0; col < xs.Length; col++)
            {
                data[row, col] = Math.Log(xs[col][row] + 1.0E-6);
            }
        }

        using (var plot = new Plot())
        {
            var heatmap = plot.Add.Heatmap(data);
            heatmap.FlipVertically = true;
            heatmap.CellWidth = (double)frameShift / sampleRate;
            heatmap.CellHeight = (double)sampleRate / frameLength;
            heatmap.CellAlignment = Alignment.LowerLeft;
            plot.XLabel("Time [s]");
            plot.YLabel("Frequency [Hz]");
            plot.SavePng("demo.png", 800, 400);
        }

        var (w, h) = NonnegativeMatrixFactorization.GetInitialGuess(xs, 3, new Random(42));

        for (var i = 0; i < 1; i++)
        {
            var w2 = w.Map(x => 0.0);
            var h2 = h.Map(h => 0.0);
            NonnegativeMatrixFactorization.Update(xs, w, h, w2, h2);
            w2.CopyTo(w);
            h2.CopyTo(h);
        }

        var reconstructed = w * h;
        for (var row = 0; row < reconstructed.RowCount; row++)
        {
            for (var col = 0; col < reconstructed.ColCount; col++)
            {
                data[row, col] = Math.Log(reconstructed[row, col] + 1.0E-6);
            }
        }

        using (var plot = new Plot())
        {
            var heatmap = plot.Add.Heatmap(data);
            heatmap.FlipVertically = true;
            heatmap.CellWidth = (double)frameShift / sampleRate;
            heatmap.CellHeight = (double)sampleRate / frameLength;
            plot.XLabel("Time [s]");
            plot.YLabel("Frequency [Hz]");
            plot.SavePng("reconstructed.png", 800, 400);
        }
    }
}
