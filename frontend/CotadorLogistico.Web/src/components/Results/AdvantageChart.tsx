import { useEffect, useRef, useState } from 'react';
import type { Chart as ChartInstance, ChartConfiguration } from 'chart.js/auto';
import { useI18n } from '../../contexts/I18nContext';
import { dateStamp } from '../../lib/format';
import type { CapitalAdvantage } from '../../types/api';

interface AdvantageChartProps {
  advantages: CapitalAdvantage[];
}

function readCssVar(name: string): string {
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
}

export function AdvantageChart({ advantages }: AdvantageChartProps) {
  const { t } = useI18n();
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const chartRef = useRef<ChartInstance | null>(null);
  const [showAbsolute, setShowAbsolute] = useState(false);

  const sorted = [...advantages].sort((a, b) => b.advantagePercent - a.advantagePercent);

  useEffect(() => {
    if (!canvasRef.current || sorted.length === 0) return;
    let disposed = false;

    const brass = readCssVar('--brass') || '#9C6B1F';
    const deep = readCssVar('--deep') || '#6B2B3A';
    const ink = readCssVar('--ink') || '#1A1A1A';
    const line = readCssVar('--line') || '#E4E3DD';

    const labels = sorted.map((a) => a.destinationLabel);
    const values = sorted.map((a) => (showAbsolute ? Number((a.melhorEnvioAvgPriceBrl - a.frenetAvgPriceBrl).toFixed(2)) : a.advantagePercent));
    const colors = values.map((v) => (v >= 0 ? brass : deep));

    const config: ChartConfiguration<'bar'> = {
      type: 'bar',
      data: {
        labels,
        datasets: [{ data: values, backgroundColor: colors, borderRadius: 3, barThickness: 16 }],
      },
      options: {
        indexAxis: 'y',
        responsive: true,
        maintainAspectRatio: false,
        animation: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (ctx) => (showAbsolute ? `R$ ${Number(ctx.raw).toFixed(2)}` : `${Number(ctx.raw).toFixed(1)}%`),
            },
          },
        },
        scales: {
          x: {
            grid: { color: line },
            ticks: { color: ink, callback: (v) => (showAbsolute ? `R$ ${v}` : `${v}%`) },
          },
          y: {
            grid: { display: false },
            ticks: { color: ink, font: { family: 'Sora', size: 11 } },
          },
        },
      },
    };

    void import('chart.js/auto').then(({ Chart }) => {
      if (disposed || !canvasRef.current) return;
      chartRef.current?.destroy();
      chartRef.current = new Chart(canvasRef.current, config);
    });

    return () => {
      disposed = true;
      chartRef.current?.destroy();
      chartRef.current = null;
    };
  }, [sorted, showAbsolute]);

  function handleExportPng() {
    if (!canvasRef.current) return;
    const url = canvasRef.current.toDataURL('image/png');
    const link = document.createElement('a');
    link.href = url;
    link.download = `vantagem-por-capital-${dateStamp()}.png`;
    link.click();
  }

  if (advantages.length === 0) return null;

  return (
    <div className="advantage-chart-card">
      <div className="advantage-chart-header">
        <div>
          <div className="advantage-chart-title">{t('chart.advantageTitle')}</div>
          <div className="field-hint">{t('chart.advantageSubtitle')}</div>
        </div>
        <div className="advantage-chart-legend">
          <span><span className="dot" style={{ background: 'var(--brass)' }} />{t('chart.frenetLabel')}</span>
          <span><span className="dot" style={{ background: 'var(--deep)' }} />{t('chart.meLabel')}</span>
        </div>
      </div>

      <div style={{ height: Math.max(220, sorted.length * 28) }}>
        <canvas ref={canvasRef} role="img" aria-label={t('chart.advantageTitle')} />
      </div>

      <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 14, flexWrap: 'wrap', gap: 10 }}>
        <button type="button" className="export-btn" onClick={() => setShowAbsolute((v) => !v)}>
          {showAbsolute ? t('chart.percentToggle') : t('chart.absoluteToggle')}
        </button>
        <button type="button" className="export-btn" onClick={handleExportPng}>
          {t('chart.exportPngButton')}
        </button>
      </div>
    </div>
  );
}
