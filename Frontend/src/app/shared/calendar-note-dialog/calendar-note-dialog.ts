import { Component, OnChanges, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CalendarNote } from '../../models/calendar-note.model';
import { CalendarNoteService } from '../../services/calendar-note.service';
import { DisplayDatePipe } from '../display-date/display-date.pipe';

@Component({
  selector: 'app-calendar-note-dialog',
  imports: [FormsModule, DisplayDatePipe],
  templateUrl: './calendar-note-dialog.html',
})
export class CalendarNoteDialog implements OnChanges {
  open = input(false);
  noteDate = input.required<string>();
  note = input<Pick<CalendarNote, 'noteDate' | 'content'> | null>(null);
  editable = input(false);
  allowDateSelection = input(false);
  closed = output<void>();
  changed = output<void>();

  content = '';
  selectedDate = '';
  loadedNote = signal<CalendarNote | null>(null);
  processing = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(private readonly noteService: CalendarNoteService) {}

  ngOnChanges(): void {
    if (this.open()) {
      this.selectedDate = this.noteDate();
      this.content = this.note()?.content ?? '';
      this.loadedNote.set(this.note() as CalendarNote | null);
      this.errorMessage.set(null);
    }
  }

  activeNote(): Pick<CalendarNote, 'noteDate' | 'content'> | null {
    return this.allowDateSelection() ? this.loadedNote() : this.note();
  }

  async dateChanged(): Promise<void> {
    if (!this.selectedDate) return;
    this.processing.set(true);
    this.errorMessage.set(null);
    try {
      const notes = await this.noteService.list(this.selectedDate, this.selectedDate);
      const note = notes[0] ?? null;
      this.loadedNote.set(note);
      this.content = note?.content ?? '';
    } catch {
      this.errorMessage.set('ไม่สามารถโหลด Note ของวันที่เลือกได้');
    } finally {
      this.processing.set(false);
    }
  }

  async save(): Promise<void> {
    if (this.allowDateSelection() && !this.selectedDate) {
      this.errorMessage.set('กรุณาเลือกวันที่');
      return;
    }
    const content = this.content.trim();
    if (!content) {
      this.errorMessage.set('กรุณากรอก Note');
      return;
    }
    this.processing.set(true);
    this.errorMessage.set(null);
    try {
      await this.noteService.save(this.selectedDate || this.noteDate(), content);
      this.changed.emit();
    } catch {
      this.errorMessage.set('ไม่สามารถบันทึก Note ได้');
    } finally {
      this.processing.set(false);
    }
  }

  async remove(): Promise<void> {
    if (!this.activeNote()) return;
    this.processing.set(true);
    this.errorMessage.set(null);
    try {
      await this.noteService.delete(this.selectedDate || this.noteDate());
      this.changed.emit();
    } catch {
      this.errorMessage.set('ไม่สามารถลบ Note ได้');
    } finally {
      this.processing.set(false);
    }
  }
}
