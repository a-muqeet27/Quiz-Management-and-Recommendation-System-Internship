using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quizportal.Data;
using quizportal.Models;

namespace quizportal.Controllers
{
    public class QuizController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuizController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> QuizList()
        {
            var quizzes = await _context.QuizInfos.ToListAsync();
            return View(quizzes);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuizInfo quiz)
        {
            if (ModelState.IsValid)
            {
                _context.QuizInfos.Add(quiz);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(QuizList));
            }

            return View(quiz);
        }

        public async Task<IActionResult> Details(int id)
        {
            var quiz = await _context.QuizInfos.FindAsync(id);

            if (quiz == null)
                return NotFound();

            return View(quiz);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var quiz = await _context.QuizInfos.FindAsync(id);

            if (quiz == null)
                return NotFound();

            return View(quiz);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, QuizInfo quiz)
        {
            if (id != quiz.QuizID)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(quiz);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(QuizList));
            }

            return View(quiz);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var quiz = await _context.QuizInfos.FindAsync(id);

            if (quiz == null)
            {
                return NotFound();
            }

            return View(quiz);
        }
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var quiz = await _context.QuizInfos.FindAsync(id);

            if (quiz != null)
            {
                _context.QuizInfos.Remove(quiz);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(QuizList));
        }
    }
}