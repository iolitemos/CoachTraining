import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AthleteDetail } from '../../../models/athlete.model';
import { ParentRoutinePlanLinkStatus } from '../../../models/parent-routine-plan.model';
import { AthleteService } from '../../../services/athlete.service';
import { ParentRoutinePlanService } from '../../../services/parent-routine-plan.service';
import { PageHeader } from '../../../shared/page-header/page-header';

@Component({ selector: 'app-athlete-plan-link', imports: [RouterLink, PageHeader], templateUrl: './athlete-plan-link.html' })
export class AthletePlanLink implements OnInit {
  athlete = signal<AthleteDetail | null>(null);
  status = signal<ParentRoutinePlanLinkStatus | null>(null);
  loading = signal(true); processing = signal(false); message = signal<string | null>(null);
  readonly athleteId: number;
  constructor(route: ActivatedRoute, private readonly athletes: AthleteService, private readonly plans: ParentRoutinePlanService) { this.athleteId = Number(route.snapshot.paramMap.get('id')); }
  ngOnInit(): void { void this.load(); }
  async load(): Promise<void> { this.loading.set(true); try { const [athlete, status] = await Promise.all([this.athletes.getById(this.athleteId), this.plans.getLinkStatus(this.athleteId)]); this.athlete.set(athlete); this.status.set(status); } catch { this.message.set('ไม่สามารถโหลดข้อมูลได้'); } finally { this.loading.set(false); } }
  url(): string | null { const token = this.status()?.token; return token ? `${window.location.origin}/parent/routine-plan/${token}` : null; }
  async rotate(): Promise<void> { if (this.status()?.exists && !window.confirm('ลิงก์เดิมจะใช้งานไม่ได้ทันที ต้องการสร้างลิงก์ใหม่หรือไม่?')) return; this.processing.set(true); try { const result = await this.plans.rotateLink(this.athleteId); this.status.set({ exists: true, isEnabled: true, ...result }); this.message.set('สร้างลิงก์เรียบร้อยแล้ว'); } catch { this.message.set('ไม่สามารถสร้างลิงก์ได้'); } finally { this.processing.set(false); } }
  async setAccess(): Promise<void> { const current = this.status(); if (!current) return; this.processing.set(true); try { this.status.set(await this.plans.setAccess(this.athleteId, !current.isEnabled)); this.message.set(!current.isEnabled ? 'เปิดการเข้าถึงแล้ว' : 'ปิดการเข้าถึงชั่วคราวแล้ว'); } catch { this.message.set('ไม่สามารถเปลี่ยนสถานะลิงก์ได้'); } finally { this.processing.set(false); } }
  async copy(): Promise<void> { const url = this.url(); if (!url) return; try { await navigator.clipboard.writeText(url); this.message.set('คัดลอกลิงก์แล้ว'); } catch { this.message.set('กรุณาเลือกลิงก์และคัดลอกด้วยตนเอง'); } }
}
